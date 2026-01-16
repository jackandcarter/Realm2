using System;
using System.Collections.Generic;
using Realm.Combat.Data;

namespace Client.Combat.Runtime
{
    public struct ComboInputContext
    {
        public ComboInputType Input;
        public bool HitConfirmed;
        public float Stamina;
    }

    public struct ComboSystemState
    {
        public string CurrentNodeId;
        public float LastInputTime;
        public ComboStepDefinition ActiveStep;
    }

    public class ComboSystem
    {
        private readonly Dictionary<ComboInputType, string> _startNodes = new();
        private readonly Dictionary<string, ComboNodeDefinition> _nodeMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<ComboEdgeDefinition>> _edgeMap =
            new(StringComparer.OrdinalIgnoreCase);

        private WeaponCombatDefinition _definition;
        private ComboSystemState _state;

        public float InputTimeoutSeconds { get; set; } = 1.1f;
        public ComboSystemState State => _state;

        public void SetDefinition(WeaponCombatDefinition definition)
        {
            if (_definition == definition)
            {
                return;
            }

            _definition = definition;
            BuildGraphCache();
            ResetCombo();
        }

        public void ResetCombo()
        {
            _state = new ComboSystemState
            {
                CurrentNodeId = string.Empty,
                LastInputTime = float.NegativeInfinity,
                ActiveStep = null
            };
        }

        public bool TryAdvanceCombo(ComboInputType input, float now, out ComboStepDefinition step)
        {
            return TryAdvanceCombo(new ComboInputContext
            {
                Input = input,
                HitConfirmed = false,
                Stamina = float.MaxValue
            }, now, out step);
        }

        public bool TryAdvanceCombo(ComboInputContext context, float now, out ComboStepDefinition step)
        {
            step = null;

            if (_definition == null || _definition.ComboGraph == null)
            {
                return false;
            }

            if (now - _state.LastInputTime > InputTimeoutSeconds)
            {
                ResetCombo();
            }

            var targetNodeId = ResolveNextNode(context);
            if (string.IsNullOrWhiteSpace(targetNodeId) || !_nodeMap.TryGetValue(targetNodeId, out var node))
            {
                ResetCombo();
                return false;
            }

            step = node.Step;
            _state.CurrentNodeId = node.NodeId;
            _state.LastInputTime = now;
            _state.ActiveStep = step;
            return step != null;
        }

        private string ResolveNextNode(ComboInputContext context)
        {
            if (string.IsNullOrWhiteSpace(_state.CurrentNodeId))
            {
                return ResolveStartNode(context.Input);
            }

            if (_edgeMap.TryGetValue(_state.CurrentNodeId, out var edges))
            {
                foreach (var edge in edges)
                {
                    if (edge == null || edge.Input != context.Input)
                    {
                        continue;
                    }

                    if (!ConditionsMet(edge.Conditions, context))
                    {
                        continue;
                    }

                    return edge.ToNodeId;
                }
            }

            return ResolveStartNode(context.Input);
        }

        private string ResolveStartNode(ComboInputType input)
        {
            return _startNodes.TryGetValue(input, out var nodeId) ? nodeId : string.Empty;
        }

        private static bool ConditionsMet(ComboEdgeConditionDefinition conditions, ComboInputContext context)
        {
            if (conditions == null)
            {
                return true;
            }

            if (conditions.HitConfirmedRequired && !context.HitConfirmed)
            {
                return false;
            }

            if (context.Stamina < conditions.StaminaMin)
            {
                return false;
            }

            return true;
        }

        private void BuildGraphCache()
        {
            _startNodes.Clear();
            _nodeMap.Clear();
            _edgeMap.Clear();

            if (_definition == null || _definition.ComboGraph == null)
            {
                return;
            }

            foreach (var startNode in _definition.ComboGraph.StartNodes)
            {
                if (startNode == null || string.IsNullOrWhiteSpace(startNode.NodeId))
                {
                    continue;
                }

                _startNodes[startNode.Input] = startNode.NodeId;
            }

            foreach (var node in _definition.ComboGraph.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                {
                    continue;
                }

                _nodeMap[node.NodeId] = node;
            }

            foreach (var edge in _definition.ComboGraph.Edges)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.FromNodeId))
                {
                    continue;
                }

                if (!_edgeMap.TryGetValue(edge.FromNodeId, out var list))
                {
                    list = new List<ComboEdgeDefinition>();
                    _edgeMap[edge.FromNodeId] = list;
                }

                list.Add(edge);
            }
        }
    }
}
