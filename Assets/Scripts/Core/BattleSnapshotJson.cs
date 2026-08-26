using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FruitDefense.Core
{
    public static class BattleSnapshotJson
    {
        public static string Serialize(BattleSnapshot snapshot, bool prettyPrint = false)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return JsonUtility.ToJson(snapshot, prettyPrint);
        }

        public static BattleSnapshotRestoreResult Deserialize(string json,
            out BattleSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(json))
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$",
                    "Snapshot JSON is empty.");
            ShapeNode root;
            try
            {
                root = new ShapeParser(json).Parse();
            }
            catch (FormatException exception)
            {
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$", exception.Message);
            }
            var rootObject = root as ShapeObject;
            if (rootObject == null)
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$",
                    "Snapshot JSON root must be an object.");

            ShapeNode versionNode;
            if (!rootObject.Members.TryGetValue("schemaVersion", out versionNode))
                return Missing("schemaVersion");
            int version;
            if (versionNode.Kind != ShapeKind.Number
                || !int.TryParse(versionNode.Scalar, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out version)
                || version != BattleSnapshotSchema.Version)
                return Failure(BattleSnapshotRestoreCode.UnsupportedSchema,
                    "schemaVersion", "Only schema version 4 is supported.");

            ShapeNode idNode;
            if (!rootObject.Members.TryGetValue("schemaId", out idNode))
                return Missing("schemaId");
            if (idNode.Kind != ShapeKind.String
                || !string.Equals(idNode.Scalar, BattleSnapshotSchema.Id,
                    StringComparison.Ordinal))
                return Failure(BattleSnapshotRestoreCode.UnsupportedSchema,
                    "schemaId", "Snapshot schema ID is unsupported.");

            var presence = ValidateCurrentShape(rootObject);
            if (!presence.Succeeded) return presence;
            try
            {
                snapshot = JsonUtility.FromJson<BattleSnapshot>(json);
            }
            catch (Exception exception)
            {
                return Failure(BattleSnapshotRestoreCode.InvalidPayload, "$", exception.Message);
            }
            return snapshot == null
                ? Failure(BattleSnapshotRestoreCode.InvalidPayload, "$",
                    "Snapshot JSON produced no envelope.")
                : BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult ValidateCurrentShape(ShapeObject root)
        {
            var result = RequireFields(root, "$", RootFields);
            if (!result.Succeeded) return result;
            result = RequireArrayElements(root, "equipment", EquipmentFields);
            if (!result.Succeeded) return result;
            result = RequireArrayElements(root, "pots", PotFields);
            if (!result.Succeeded) return result;
            result = RequireArrayElements(root, "plants", PlantFields);
            if (!result.Succeeded) return result;
            result = RequireArrayElements(root, "enemies", EnemyFields);
            if (!result.Succeeded) return result;
            result = RequireArrayElements(root, "projectiles", ProjectileFields);
            if (!result.Succeeded) return result;

            var projectiles = (ShapeArray)root.Members["projectiles"];
            for (var index = 0; index < projectiles.Items.Count; index++)
            {
                var projectile = (ShapeObject)projectiles.Items[index];
                var hits = (ShapeArray)projectile.Members["hitEntityIds"];
                for (var hitIndex = 0; hitIndex < hits.Items.Count; hitIndex++)
                    if (hits.Items[hitIndex].Kind != ShapeKind.Number)
                        return WrongKind("projectiles[" + index + "].hitEntityIds["
                            + hitIndex + "]", ShapeKind.Number);
            }

            var runtime = (ShapeObject)root.Members["combatRuntime"];
            result = RequireFields(runtime, "combatRuntime", CombatRuntimeFields);
            if (!result.Succeeded) return result;
            var entities = (ShapeArray)runtime.Members["entities"];
            for (var index = 0; index < entities.Items.Count; index++)
            {
                var path = "combatRuntime.entities[" + index + "]";
                var entity = entities.Items[index] as ShapeObject;
                if (entity == null) return WrongKind(path, ShapeKind.Object);
                result = RequireFields(entity, path, EntityRuntimeFields);
                if (!result.Succeeded) return result;
                result = RequireArrayItems((ShapeArray)entity.Members["abilities"],
                    path + ".abilities", AbilityFields);
                if (!result.Succeeded) return result;
                result = RequireArrayItems((ShapeArray)entity.Members["statuses"],
                    path + ".statuses", StatusFields);
                if (!result.Succeeded) return result;
            }
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult RequireArrayElements(ShapeObject root,
            string name, FieldRule[] fields)
        {
            return RequireArrayItems((ShapeArray)root.Members[name], name, fields);
        }

        private static BattleSnapshotRestoreResult RequireArrayItems(ShapeArray array,
            string path, FieldRule[] fields)
        {
            for (var index = 0; index < array.Items.Count; index++)
            {
                var itemPath = path + "[" + index + "]";
                var item = array.Items[index] as ShapeObject;
                if (item == null) return WrongKind(itemPath, ShapeKind.Object);
                var result = RequireFields(item, itemPath, fields);
                if (!result.Succeeded) return result;
            }
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult RequireFields(ShapeObject value,
            string path, FieldRule[] rules)
        {
            foreach (var rule in rules)
            {
                ShapeNode child;
                var childPath = path == "$" ? rule.Name : path + "." + rule.Name;
                if (!value.Members.TryGetValue(rule.Name, out child)) return Missing(childPath);
                if (child.Kind != rule.Kind) return WrongKind(childPath, rule.Kind);
            }
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult Missing(string path)
        {
            return Failure(BattleSnapshotRestoreCode.MissingRequiredField, path,
                "Required snapshot field is missing.");
        }

        private static BattleSnapshotRestoreResult WrongKind(string path, ShapeKind expected)
        {
            return Failure(BattleSnapshotRestoreCode.InvalidPayload, path,
                "Snapshot field must be a JSON " + expected.ToString().ToLowerInvariant() + ".");
        }

        private static BattleSnapshotRestoreResult Failure(BattleSnapshotRestoreCode code,
            string path, string message)
        {
            return new BattleSnapshotRestoreResult(code, path, message);
        }

        private static FieldRule String(string name) { return new FieldRule(name, ShapeKind.String); }
        private static FieldRule Number(string name) { return new FieldRule(name, ShapeKind.Number); }
        private static FieldRule Boolean(string name) { return new FieldRule(name, ShapeKind.Boolean); }
        private static FieldRule Array(string name) { return new FieldRule(name, ShapeKind.Array); }
        private static FieldRule Object(string name) { return new FieldRule(name, ShapeKind.Object); }

        private static readonly FieldRule[] RootFields =
        {
            String("schemaId"), Number("schemaVersion"), String("levelCatalogId"),
            String("contentCatalogId"), String("contentVersion"), String("levelId"),
            String("mapId"), String("gameplayMapFingerprint"), String("waveSetId"),
            String("ruleSetId"), String("themeId"),
            String("resolvedSourceDefinitionFingerprint"), Number("logicStep"),
            Number("randomState"), Number("randomSeed"), Number("phase"), Boolean("paused"),
            Number("speed"), Number("elapsed"), Number("sun"), Number("lives"),
            Number("refreshCount"), Number("waveIndex"), Number("waveSpawned"),
            Number("waveTotal"), Number("spawnCooldown"), Number("betweenTimer"),
            Number("escapedEnemyCount"), Number("nextEntityId"),
            Number("nextStatusSequence"), Number("availablePots"), Array("equipment"),
            Array("pots"), Array("plants"), Array("enemies"), Array("projectiles"),
            Object("combatRuntime"),
        };

        private static readonly FieldRule[] EquipmentFields =
        {
            String("definitionId"), Number("count"),
        };

        private static readonly FieldRule[] PotFields =
        {
            Number("entityId"), Number("cellX"), Number("cellY"), Boolean("active"),
        };

        private static readonly FieldRule[] PlantFields =
        {
            Number("entityId"), String("definitionId"), Number("star"),
            Number("potEntityId"), Number("nurseryIndex"),
            String("equipmentDefinitionId"), Number("moveCooldown"),
        };

        private static readonly FieldRule[] EnemyFields =
        {
            Number("entityId"), String("definitionId"), Number("hp"), Number("maxHp"),
            Number("speed"), Number("pathProgress"), Number("reward"), Number("threat"),
        };

        private static readonly FieldRule[] ProjectileFields =
        {
            Number("entityId"), Number("sourceEntityId"), Number("targetEntityId"),
            String("definitionId"), String("sourceDefinitionId"),
            String("sourceEquipmentId"), String("abilityId"), Number("deliveryIndex"),
            Number("originX"), Number("originY"), Number("positionX"), Number("positionY"),
            Number("targetX"), Number("targetY"), Number("directionX"), Number("directionY"),
            Number("maxDistance"), Number("progress"), Boolean("returning"),
            Number("damageBasis"), Number("ticksRemaining"), Number("flightTicks"),
            Array("hitEntityIds"),
        };

        private static readonly FieldRule[] CombatRuntimeFields =
        {
            Number("nextCombatEventSequence"), Array("entities"),
        };

        private static readonly FieldRule[] EntityRuntimeFields =
        {
            Number("entityId"), Array("abilities"), Array("statuses"),
        };

        private static readonly FieldRule[] AbilityFields =
        {
            String("definitionId"), Number("phase"), Number("cooldownTicks"),
            Number("periodicProgressTicks"), Number("windupTicksRemaining"),
            Number("recoveryTicksRemaining"), Number("burstShotsRemaining"),
            Number("burstIntervalTicks"), Number("pendingSourceEntityId"),
            Number("pendingTargetEntityId"), Number("pendingEventMagnitude"),
            Number("pendingRootEventSequence"), Number("lastRootEventSequence"),
        };

        private static readonly FieldRule[] StatusFields =
        {
            String("definitionId"), Number("sourceEntityId"), Number("remainingTicks"),
            Number("stackCount"), Number("magnitude"), Number("sequence"),
            Number("tickProgress"),
        };

        private readonly struct FieldRule
        {
            public string Name { get; }
            public ShapeKind Kind { get; }

            public FieldRule(string name, ShapeKind kind)
            {
                Name = name;
                Kind = kind;
            }
        }

        private enum ShapeKind { Object, Array, String, Number, Boolean, Null }

        private abstract class ShapeNode
        {
            public ShapeKind Kind { get; }
            public string Scalar { get; }

            protected ShapeNode(ShapeKind kind, string scalar = null)
            {
                Kind = kind;
                Scalar = scalar;
            }
        }

        private sealed class ShapeObject : ShapeNode
        {
            public Dictionary<string, ShapeNode> Members { get; }

            public ShapeObject(Dictionary<string, ShapeNode> members) : base(ShapeKind.Object)
            {
                Members = members;
            }
        }

        private sealed class ShapeArray : ShapeNode
        {
            public List<ShapeNode> Items { get; }

            public ShapeArray(List<ShapeNode> items) : base(ShapeKind.Array)
            {
                Items = items;
            }
        }

        private sealed class ShapeScalar : ShapeNode
        {
            public ShapeScalar(ShapeKind kind, string value = null) : base(kind, value) { }
        }

        private sealed class ShapeParser
        {
            private readonly string _json;
            private int _index;

            public ShapeParser(string json) { _json = json; }

            public ShapeNode Parse()
            {
                SkipWhitespace();
                var value = ParseValue();
                SkipWhitespace();
                if (_index != _json.Length) throw Error("Unexpected trailing JSON content.");
                return value;
            }

            private ShapeNode ParseValue()
            {
                SkipWhitespace();
                if (_index >= _json.Length) throw Error("Unexpected end of JSON.");
                switch (_json[_index])
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return new ShapeScalar(ShapeKind.String, ParseString());
                    case 't': ReadLiteral("true"); return new ShapeScalar(ShapeKind.Boolean, "true");
                    case 'f': ReadLiteral("false"); return new ShapeScalar(ShapeKind.Boolean, "false");
                    case 'n': ReadLiteral("null"); return new ShapeScalar(ShapeKind.Null);
                    default: return new ShapeScalar(ShapeKind.Number, ParseNumber());
                }
            }

            private ShapeObject ParseObject()
            {
                _index++;
                var members = new Dictionary<string, ShapeNode>(StringComparer.Ordinal);
                SkipWhitespace();
                if (Take('}')) return new ShapeObject(members);
                while (true)
                {
                    SkipWhitespace();
                    if (_index >= _json.Length || _json[_index] != '"')
                        throw Error("Object member name must be a string.");
                    var name = ParseString();
                    SkipWhitespace();
                    if (!Take(':')) throw Error("Object member is missing ':'.");
                    if (members.ContainsKey(name)) throw Error("Duplicate object member '" + name + "'.");
                    members.Add(name, ParseValue());
                    SkipWhitespace();
                    if (Take('}')) return new ShapeObject(members);
                    if (!Take(',')) throw Error("Object member is missing ','.");
                }
            }

            private ShapeArray ParseArray()
            {
                _index++;
                var items = new List<ShapeNode>();
                SkipWhitespace();
                if (Take(']')) return new ShapeArray(items);
                while (true)
                {
                    items.Add(ParseValue());
                    SkipWhitespace();
                    if (Take(']')) return new ShapeArray(items);
                    if (!Take(',')) throw Error("Array item is missing ','.");
                }
            }

            private string ParseString()
            {
                if (!Take('"')) throw Error("String is missing opening quote.");
                var result = new StringBuilder();
                while (_index < _json.Length)
                {
                    var value = _json[_index++];
                    if (value == '"') return result.ToString();
                    if (value < 0x20) throw Error("String contains an unescaped control character.");
                    if (value != '\\')
                    {
                        result.Append(value);
                        continue;
                    }
                    if (_index >= _json.Length) throw Error("String escape is incomplete.");
                    var escape = _json[_index++];
                    switch (escape)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u': result.Append(ParseUnicode()); break;
                        default: throw Error("String escape is invalid.");
                    }
                }
                throw Error("String is missing closing quote.");
            }

            private char ParseUnicode()
            {
                if (_index + 4 > _json.Length) throw Error("Unicode escape is incomplete.");
                int value;
                if (!int.TryParse(_json.Substring(_index, 4), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out value))
                    throw Error("Unicode escape is invalid.");
                _index += 4;
                return (char)value;
            }

            private string ParseNumber()
            {
                var start = _index;
                if (Take('-')) { }
                if (_index >= _json.Length) throw Error("Number is incomplete.");
                if (_json[_index] == '0') _index++;
                else
                {
                    if (!char.IsDigit(_json[_index])) throw Error("JSON value is invalid.");
                    while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                }
                if (Take('.'))
                {
                    if (_index >= _json.Length || !char.IsDigit(_json[_index]))
                        throw Error("Number fraction is incomplete.");
                    while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                }
                if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
                {
                    _index++;
                    if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-'))
                        _index++;
                    if (_index >= _json.Length || !char.IsDigit(_json[_index]))
                        throw Error("Number exponent is incomplete.");
                    while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                }
                return _json.Substring(start, _index - start);
            }

            private void ReadLiteral(string literal)
            {
                if (_index + literal.Length > _json.Length
                    || !string.Equals(_json.Substring(_index, literal.Length), literal,
                        StringComparison.Ordinal))
                    throw Error("JSON literal is invalid.");
                _index += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index])) _index++;
            }

            private bool Take(char expected)
            {
                if (_index >= _json.Length || _json[_index] != expected) return false;
                _index++;
                return true;
            }

            private FormatException Error(string message)
            {
                return new FormatException(message + " Offset "
                    + _index.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }
    }
}
