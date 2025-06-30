using System;
using System.Collections.Generic;

namespace Ouroboros.StdLib.Math
{
    /// <summary>
    /// Extension methods for set operations with Unicode operators
    /// </summary>
    public static class SetOperations
    {
        /// <summary>
        /// Union operator (∪)
        /// </summary>
        public static HashSet<T> Union<T>(this HashSet<T> left, HashSet<T> right)
        {
            var result = new HashSet<T>(left);
            result.UnionWith(right);
            return result;
        }
        
        /// <summary>
        /// Intersection operator (∩)
        /// </summary>
        public static HashSet<T> Intersection<T>(this HashSet<T> left, HashSet<T> right)
        {
            var result = new HashSet<T>(left);
            result.IntersectWith(right);
            return result;
        }
        
        /// <summary>
        /// Element of operator (∈)
        /// </summary>
        public static bool ElementOf<T>(this T element, HashSet<T> set)
        {
            return set.Contains(element);
        }
        
        /// <summary>
        /// Not element of operator (∉)
        /// </summary>
        public static bool NotElementOf<T>(this T element, HashSet<T> set)
        {
            return !set.Contains(element);
        }
        
        /// <summary>
        /// Set difference operator (∖)
        /// </summary>
        public static HashSet<T> Difference<T>(this HashSet<T> left, HashSet<T> right)
        {
            var result = new HashSet<T>(left);
            result.ExceptWith(right);
            return result;
        }
        
        /// <summary>
        /// Symmetric difference operator (△)
        /// </summary>
        public static HashSet<T> SymmetricDifference<T>(this HashSet<T> left, HashSet<T> right)
        {
            var result = new HashSet<T>(left);
            result.SymmetricExceptWith(right);
            return result;
        }
        
        /// <summary>
        /// Subset operator (⊂)
        /// </summary>
        public static bool ProperSubset<T>(this HashSet<T> left, HashSet<T> right)
        {
            return left.IsProperSubsetOf(right);
        }
        
        /// <summary>
        /// Superset operator (⊃)
        /// </summary>
        public static bool ProperSuperset<T>(this HashSet<T> left, HashSet<T> right)
        {
            return left.IsProperSupersetOf(right);
        }
        
        /// <summary>
        /// Subset or equal operator (⊆)
        /// </summary>
        public static bool SubsetOrEqual<T>(this HashSet<T> left, HashSet<T> right)
        {
            return left.IsSubsetOf(right);
        }
        
        /// <summary>
        /// Superset or equal operator (⊇)
        /// </summary>
        public static bool SupersetOrEqual<T>(this HashSet<T> left, HashSet<T> right)
        {
            return left.IsSupersetOf(right);
        }
    }
} 