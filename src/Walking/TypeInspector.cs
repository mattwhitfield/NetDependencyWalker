using System;
using System.Collections.Generic;

namespace NetDependencyWalker.Walking
{
    using System.Linq;
    using NetDependencyWalker.ViewModel;
    using Mono.Cecil;

    internal class TypeInspector
    {
        private readonly OrderingType _orderingType;

        public TypeInspector(OrderingType orderingType)
        {
            _orderingType = orderingType;
        }

        readonly Dictionary<string, ReferencedTypeNode> _referencedTypeNodes = new Dictionary<string, ReferencedTypeNode>();

        public IList<ReferencedTypeNode> CalculateReferencedTypes(string sourcePath, string targetPath)
        {
            var child = AssemblyDefinition.ReadAssembly(targetPath);
            var parent = AssemblyDefinition.ReadAssembly(sourcePath);

            var resultNodes = new List<ReferencedTypeNode>();

            

            foreach (var type in parent.MainModule.Types)
            {
                CheckAndAddTypeReference(type.BaseType, type, ReferencingMemberType.TypeHierarchy, string.Empty, child, false);

                foreach (var fieldDefinition in type.Fields)
                {
                    if (fieldDefinition.IsSpecialName || fieldDefinition.IsRuntimeSpecialName || fieldDefinition.Name.EndsWith("__BackingField", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    CheckAndAddTypeReference(fieldDefinition.FieldType, type, ReferencingMemberType.Field, fieldDefinition.Name, child, false);
                }

                foreach (var propertyDefinition in type.Properties)
                {
                    CheckAndAddTypeReference(propertyDefinition.PropertyType, type, ReferencingMemberType.Property, propertyDefinition.Name, child, false);
                    if (propertyDefinition.GetMethod != null)
                    {
                        ScanMethodBody(propertyDefinition.GetMethod, type, ReferencingMemberType.Property, propertyDefinition.Name, child);
                    }
                    if (propertyDefinition.SetMethod != null)
                    {
                        ScanMethodBody(propertyDefinition.SetMethod, type, ReferencingMemberType.Property, propertyDefinition.Name, child);
                    }
                }

                foreach (var methodDefinition in type.Methods)
                {
                    if (methodDefinition.IsGetter || methodDefinition.IsSetter)
                    {
                        continue;
                    }

                    CheckAndAddTypeReference(methodDefinition.ReturnType, type, ReferencingMemberType.Method, methodDefinition.Name, child, false);
                    ScanMethodBody(methodDefinition, type, ReferencingMemberType.Method, methodDefinition.Name, child);
                }
            }


            foreach (var node in _referencedTypeNodes.Values.OrderBy(x => x.Namespace + "." + x.ClassName))
            {
                Clean(node);

                resultNodes.Add(node);
            }

            return resultNodes;
        }

        private void Clean(ReferencedTypeNode node)
        {
            foreach (var childNode in node.Children)
            {
                Clean(childNode);
            }

            node.Children.Sort((x, y) => string.Compare(x.Initial + x.Secondary, y.Initial + y.Secondary, StringComparison.Ordinal));
        }

        void ReferencedThenReferencingThenMember(TypeReference typeReference, string ns, TypeDefinition typeDefinition, string memberName, ReferencingMemberType referencingMemberType, bool isBodyReference)
        {
            var key = typeReference.Scope.Name + ":" + typeReference.FullName;
            if (!_referencedTypeNodes.TryGetValue(key, out var referencedTypeNode))
            {
                referencedTypeNode = new ReferencedTypeNode(ns, typeReference.FullName.Substring(ns.Length + 1), null, null);
                _referencedTypeNodes[key] = referencedTypeNode;
            }

            var childReferencingTypeNode = referencedTypeNode.Children.FirstOrDefault(x => x.Namespace == typeDefinition.Namespace && x.ClassName == typeDefinition.Name);
            if (childReferencingTypeNode == null)
            {
                childReferencingTypeNode = new ReferencedTypeNode(typeDefinition.Namespace, typeDefinition.Name, false, typeDefinition.Name + " contains references to " + typeReference.FullName.Substring(ns.Length + 1));
                referencedTypeNode.Children.Add(childReferencingTypeNode);
            }

            if (!childReferencingTypeNode.Children.Any(x => x.MemberName == memberName && x.MemberType == referencingMemberType))
            {
                var description = GetReferenceDescription(typeReference, ns, typeDefinition, memberName, referencingMemberType, isBodyReference);

                childReferencingTypeNode.Children.Add(new ReferencedTypeNode(referencingMemberType, memberName, description));
            }
        }

        private static string GetReferenceDescription(TypeReference typeReference, string ns, TypeDefinition typeDefinition, string memberName, ReferencingMemberType referencingMemberType, bool isBodyReference)
        {
            if (isBodyReference)
            {
                return "The implementation of " + typeDefinition.Name + "." + memberName + " contains references to " + typeReference.FullName.Substring(ns.Length + 1);
            }

            if (referencingMemberType == ReferencingMemberType.Field || referencingMemberType == ReferencingMemberType.Property)
            {
                return typeDefinition.Name + "." + memberName + " is of a type that is or includes " + typeReference.FullName.Substring(ns.Length + 1);
            }

            if (referencingMemberType == ReferencingMemberType.Method)
            {
                return typeDefinition.Name + "." + memberName + " has a return type that is or includes " + typeReference.FullName.Substring(ns.Length + 1);
            }

            if (referencingMemberType == ReferencingMemberType.TypeHierarchy)
            {
                return typeDefinition.Name + " inherits a type that is or includes " + typeReference.FullName.Substring(ns.Length + 1);
            }

            return null;
        }

        void ReferencingThenReferencedThenMember(TypeReference typeReference, string ns, TypeDefinition typeDefinition, string memberName, ReferencingMemberType referencingMemberType, bool isBodyReference)
        {
            var key = typeDefinition.FullName;
            if (!_referencedTypeNodes.TryGetValue(key, out var referencedTypeNode))
            {
                referencedTypeNode = new ReferencedTypeNode(typeDefinition.Namespace, typeDefinition.Name, null, null);
                _referencedTypeNodes[key] = referencedTypeNode;
            }

            var memberNode = referencedTypeNode.Children.FirstOrDefault(x => x.MemberName == memberName && x.MemberType == referencingMemberType);
            if (memberNode == null)
            {
                memberNode = new ReferencedTypeNode(referencingMemberType, memberName, null);
                referencedTypeNode.Children.Add(memberNode);
            }

            var description = GetReferenceDescription(typeReference, ns, typeDefinition, memberName, referencingMemberType, isBodyReference);
            var childReferencingType = new ReferencedTypeNode(ns, typeReference.FullName.Substring(ns.Length + 1), true, description);
            var childReferencingTypeNode = memberNode.Children.FirstOrDefault(x => x.Namespace == childReferencingType.Namespace && x.ClassName == childReferencingType.ClassName);
            if (childReferencingTypeNode == null)
            {
                memberNode.Children.Add(childReferencingType);
            }
        }

        void ReferencingThenReferenced(TypeReference typeReference, string ns, TypeDefinition typeDefinition)
        {
            var key = typeDefinition.FullName;
            if (!_referencedTypeNodes.TryGetValue(key, out var referencedTypeNode))
            {
                referencedTypeNode = new ReferencedTypeNode(typeDefinition.Namespace, typeDefinition.Name, null, null);
                _referencedTypeNodes[key] = referencedTypeNode;
            }

            var childReferencingType = new ReferencedTypeNode(ns, typeReference.FullName.Substring(ns.Length + 1), true, typeDefinition.Name + " contains references to " + typeReference.FullName.Substring(ns.Length + 1));
            var childReferencingTypeNode = referencedTypeNode.Children.FirstOrDefault(x => x.Namespace == childReferencingType.Namespace && x.ClassName == childReferencingType.ClassName);
            if (childReferencingTypeNode == null)
            {
                referencedTypeNode.Children.Add(childReferencingType);
            }
        }

        void AddReference(TypeReference referencedType, TypeDefinition referencingType, ReferencingMemberType memberType, string memberName, bool isBodyReference)
        {
            var @namespace = referencedType.Namespace;
            var current = referencedType;
            while (current != null && current.IsNested)
            {
                if (current.DeclaringType != null)
                {
                    @namespace = current.DeclaringType.Namespace;
                }

                current = current.DeclaringType;
            }

            switch (_orderingType)
            {
                case OrderingType.ReferencedThenReferencingThenMember:
                    ReferencedThenReferencingThenMember(referencedType, @namespace, referencingType, memberName, memberType, isBodyReference);
                    break;
                case OrderingType.ReferencingThenMemberThenReferenced:
                    ReferencingThenReferencedThenMember(referencedType, @namespace, referencingType, memberName, memberType, isBodyReference);
                    break;
                case OrderingType.ReferencingThenReferenced:
                    ReferencingThenReferenced(referencedType, @namespace, referencingType);
                    break;
            }
        }

        void CheckAndAddTypeReference(TypeReference referencedType, TypeDefinition referencingType, ReferencingMemberType memberType, string memberName, AssemblyDefinition child, bool isBodyReference)
        {
            if (referencedType == null)
            {
                return;
            }

            if (referencedType.IsNested)
            {
                CheckAndAddTypeReference(referencedType.DeclaringType, referencingType, memberType, memberName, child, isBodyReference);
                return;
            }

            if (referencedType.IsGenericInstance)
            {
                GenericInstanceType instance = (GenericInstanceType)referencedType;
                foreach (var genericArgument in instance.GenericArguments)
                {
                    CheckAndAddTypeReference(genericArgument, referencingType, memberType, memberName, child, isBodyReference);
                }
            }

            if (referencedType.Name.StartsWith("!"))
            {
                return;
            }

            if (referencedType.Scope.Name != child.Name.Name)
            {
                return;
            }

            AddReference(referencedType, referencingType, memberType, memberName, isBodyReference);
        }

        void ScanMethodBody(MethodDefinition methodDefinition, TypeDefinition referencingType, ReferencingMemberType memberType, string memberName, AssemblyDefinition child)
        {
            if (methodDefinition.Body == null)
            {
                return;
            }

            foreach (var operand in methodDefinition.Body.Instructions.Select(i => i.Operand))
            {
                if (operand is TypeReference typeRef)
                {
                    CheckAndAddTypeReference(typeRef, referencingType, memberType, memberName, child, true);
                }
                else if (operand is FieldReference fieldRef)
                {
                    CheckAndAddTypeReference(fieldRef.FieldType, referencingType, memberType, memberName, child, true);
                }
                else if (operand is PropertyReference propertyRef)
                {
                    CheckAndAddTypeReference(propertyRef.PropertyType, referencingType, memberType, memberName, child, true);
                }
                else if (operand is MethodReference methodReference)
                {
                    CheckAndAddTypeReference(methodReference.ReturnType, referencingType, memberType, memberName, child, true);
                }
            }
        }

    }
}
