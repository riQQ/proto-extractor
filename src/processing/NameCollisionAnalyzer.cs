﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using protoextractor.IR;
using System.Security.Cryptography;

namespace protoextractor.processing
{
    // Care casing!
    // Namespace names are cased in lowercase.
    // Class types are cased in PascalCase.
    // Enum types are cased in PascalCase.
    //      Class Property names are snake_cased.
    //      Enum property names are in UPPERCASE.
    class NameCollisionAnalyzer
    {
        IRProgram _program;

        MD5 _md5Hash;

        public NameCollisionAnalyzer(IRProgram program)
        {
            _program = program;
            _md5Hash = MD5.Create();
        }

        public IRProgram Process()
        {
            // Check if there are duplicate private types within the same class.
            TestPrivateTypeCollision();

            // Test public enums properties.
            TestCollisionEnumProperties();

            // Check if there are not multiple types with the same shortname
            // inside each namespace.
            foreach (var ns in _program.Namespaces)
            {
                TestCollisionsWithin(ns);
            }

            // Check that subnamespaces don't collide with names of types 
            // from parent namespace.
            TestInterNSCollisions();

            return _program;
        }

        private void TestPrivateTypeCollision()
        {
            // Loop all classes with private types.
            foreach (var ns in _program.Namespaces)
            {
                var targetClasses = ns.Classes.Where(c => c.PrivateTypes.Count > 0);
                foreach (var irClass in targetClasses)
                {
                    // Find all private types which collide.
                    var collisions = irClass.PrivateTypes.GroupBy(x => x.ShortName)
                                                            .Where(group => group.Count() > 1)
                                                            .Select(group => group.Key);
                    foreach (var collision in collisions)
                    {
                        // Collect all types that collide.
                        var typeList = irClass.PrivateTypes.Where(t => t.ShortName.Equals(collision)).ToList();
                        // Remove the first occurrence, because only secondary occurrences must be renamed.
                        typeList.RemoveAt(0);
                        // Rename secondary occurrences.
                        RenameTypes(typeList);
                    }
                }
            }
        }

        private void TestCollisionEnumProperties()
        {
            // Public enums have there properties scoped by the parent namespace.
            // To avoid enum property collisions, we must check collisions accross all
            // property names per namespace.
            foreach (var ns in _program.Namespaces)
            {
                // Fetch all properties of ALL (public + private) enums.
                var props = ns.Enums.SelectMany(e => e.Properties);
                // Get list of collided property names.
                var collisions = props.GroupBy(e => e.Name)
                                        .Where(group => group.Count() > 1)
                                        .Select(group => group.Key);

                foreach (var collision in collisions)
                {
                    // Get all properties of enums for the current namespace matching 
                    // the collision as name.
                    var renameProperties = props.Where(p => p.Name.Equals(collision)).ToList();
                    // Don't rename the first property.
                    renameProperties.RemoveAt(0);
                    // Rename secondary occurrences of properties.
                    RenameProperties(renameProperties);
                }
            }
        }

        // Test for shortname collisions within one namespace.
        // This does NOT test for nested type collisions!
        private void TestCollisionsWithin(IRNamespace ns)
        {
            // Throw all names of types in one list.
            // Only select the public types, because we currently don't care about private/nested types.
            List<string> allShortNames = new List<string>();
            var classEnumeration = ns.Classes.Where(c => c.IsPrivate == false).Select(c => c.ShortName);
            var enumEnumeration = ns.Enums.Where(e => e.IsPrivate == false).Select(e => e.ShortName);

            allShortNames.AddRange(classEnumeration);
            allShortNames.AddRange(enumEnumeration);

            // Generate a set of unique elements from the collection.
            // If the amount of elements doesn't match, there is a name collision.
            var distinctSet = allShortNames.Distinct();
            if (distinctSet.Count() != allShortNames.Count())
            {
                // Solve the name collision..
                SolveCollisionsWithin(ns, allShortNames, distinctSet.ToList());
                // And rerun the test on the same namespace.
                TestCollisionsWithin(ns);
            }
        }

        // Solves name collisions between types in one namespace.
        private void SolveCollisionsWithin(IRNamespace ns, List<string> allShortNames,
            List<string> distinctShortNames)
        {
            // Find all types which collide.
            var collisions = allShortNames.GroupBy(x => x)
                                        .Where(group => group.Count() > 1)
                                        .Select(group => group.Key);
            foreach (var collisionName in collisions)
            {
                // Find all types matching the collision name.
                // NO case mismatch!
                var classesEnumeration = ns.Classes.Where(c => c.ShortName.Equals(collisionName));
                var enumEnumeration = ns.Enums.Where(e => e.ShortName.Equals(collisionName));
                // Throw them together in one list.
                List<IRTypeNode> collidedTypes = new List<IRTypeNode>();
                collidedTypes.AddRange(classesEnumeration);
                collidedTypes.AddRange(enumEnumeration);
                // Remove the first type in the list, because ONE item is allowed to remain 
                // untouched. The others need to have their name changed.
                collidedTypes.RemoveAt(0);
                // Rename collided types.
                RenameTypes(collidedTypes);
            }

        }

        private void TestInterNSCollisions()
        {
            // For each namespace, find all parent namespace.
            // Check if the parent namespaces don't have a type that is named 
            // after one of it's child namespaces.
            foreach (var ns in _program.Namespaces)
            {
                // Find parent namespaces.
                var parents = NameCollisionHelper.FindNSParents(_program, ns);
                foreach (var parentNS in parents)
                {
                    // Calculate the differrence in namespace names.
                    var parentNSNameCount = parentNS.FullName.Count();
                    var childName = ns.FullName.Substring(parentNSNameCount).Trim('.');

                    // If the parent namespace has a type, with a shortname, that matches
                    // the calculated childName. That child has to be renamed!
                    // Case mismatch! PascalCase <-> lowercase
                    var foundClasses = parentNS.Classes.Where(c => c.ShortName.ToLower().Equals(childName));
                    var foundEnums = parentNS.Enums.Where(e => e.ShortName.ToLower().Equals(childName));

                    // Throw all these types together.
                    List<IRTypeNode> typeAgg = new List<IRTypeNode>();
                    typeAgg.AddRange(foundClasses);
                    typeAgg.AddRange(foundEnums);
                    // Fix names.
                    RenameTypes(typeAgg);
                }
            }
        }

        private void RenameTypes(IEnumerable<IRTypeNode> types)
        {
            foreach (var type in types)
            {
                // Prepare affix, this has to be somewhat deterministic so we use the hash
                // of the fullName.
                var typeHash = GetMD5Hash(type.FullName);
                // Take first 3 characters from the hash.
                typeHash = typeHash.Substring(0, 3);

                var affix = string.Format("_a{0}", typeHash);
                // Append affix to full and shortname.
                type.FullName = type.FullName + affix;
                type.ShortName = type.ShortName + affix;
            }
        }

        private void RenameProperties(IEnumerable<IREnumProperty> properties)
        {
            foreach (var prop in properties)
            {
                // Prepare affix, this has to be somewhat deterministic, so resolve fullName of the
                // parent enum (which we have to find :/)
                var parentEnum = _program.Namespaces.SelectMany(ns => ns.Enums)
                                                        .First(e => e.Properties.Contains(prop));
                // Use the parent enum fullname to construct our hash.
                var typeHash = GetMD5Hash(parentEnum.FullName);
                // Take first 3 characters from the hash.
                typeHash = typeHash.Substring(0, 3);

                var affix = string.Format("_a{0}", typeHash);
                // Append affix to full and shortname.
                prop.Name = prop.Name + affix;
            }
        }

        private string GetMD5Hash(string input)
        {
            // Example taken from: https://msdn.microsoft.com/en-us/library/s02tk69a(v=vs.110).aspx
            // Construct hash.
            byte[] data = _md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Generate hexadecimal representation of hash.
            StringBuilder builder = new StringBuilder();
            foreach (var b in data)
            {
                builder.Append(b.ToString("x2"));
            }
            // Return hex string.
            return builder.ToString();
        }
    }

    public static class NameCollisionHelper
    {
        public static List<IRNamespace> FindNSParents(IRProgram program, IRNamespace subject)
        {
            // Return all namespaces whos fullname are found at the beginnen of the subject
            // namespace.
            var subjName = subject.FullName;
            var parents = program.Namespaces.Where(p => subjName.StartsWith(p.FullName)).ToList();
            // Remove subject, because fullname matches always with itself.
            parents.Remove(subject);
            return parents;
        }
    }
}
