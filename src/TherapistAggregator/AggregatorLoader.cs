using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TherapistAggregator
{
    public class AggregatorLoader
    {
        public IList<ITherapistAggregator> LoadAggregators()
        {
            List<ITherapistAggregator> aggregators = new List<ITherapistAggregator>();
            var directoryName = Path.GetDirectoryName(Assembly.GetAssembly(typeof(AggregatorLoader)).Location);
            Debug.Assert(directoryName != null);
            var directoryInfo = new DirectoryInfo(directoryName).GetParent().GetParent().GetParent();
            var dlls = directoryInfo.EnumerateFiles("*.dll", SearchOption.AllDirectories).Where(f => !f.FullName.Contains("\\packages"));
            foreach (var dll in dlls)
            {
                var fullName = dll.FullName;
                IEnumerable<ITherapistAggregator> aggregatorsFromFile = LoadAggregators(fullName);
                foreach (var therapistAggregator in aggregatorsFromFile)
                {
                    if (aggregators.Any(a => TypeEquals(a, therapistAggregator)))
                        continue;
                    aggregators.Add(therapistAggregator);
                }
            }

            return aggregators;
        }

        private ICollection<Type> GetMatchingTypesInAssembly(Assembly assembly, Predicate<Type> predicate)
        {
            ICollection<Type> types = new List<Type>();
            try
            {
                types = assembly.GetTypes().Where(i => i != null && predicate(i) && i.Assembly == assembly).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Type theType in ex.Types)
                {
                    try
                    {
                        if (theType != null && predicate(theType) && theType.Assembly == assembly)
                            types.Add(theType);
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }
            }

            return types;
        }

        private bool TypeEquals(ITherapistAggregator a1, ITherapistAggregator a2)
        {
            var type1 = a1.GetType();
            var type2 = a2.GetType();
            if (type1.Assembly.FullName != type2.Assembly.FullName)
                return false;
            if (type1.FullName != type2.FullName)
                return false;
            if (type1.AssemblyQualifiedName != type2.AssemblyQualifiedName)
                return false;
            return true;
        }

        private IEnumerable<ITherapistAggregator> LoadAggregators(string dll)
        {
            Assembly loadedAssembly;
            try
            {
                loadedAssembly = Assembly.LoadFile(dll);
            }
            catch
            {
                return Enumerable.Empty<ITherapistAggregator>();
            }

            var types = GetMatchingTypesInAssembly(loadedAssembly, IsTypeTherapistAggregator);
            var aggregators = types.Select(t => (ITherapistAggregator)Activator.CreateInstance(t));
            return aggregators;
        }

        private bool IsTypeTherapistAggregator(Type t)
        {
            var isAssignableFrom = typeof(ITherapistAggregator).IsAssignableFrom(t);
            return !t.IsAbstract
                   && t.IsClass
                   && !t.IsInterface
                   && t.IsPublic
                   && isAssignableFrom;
        }
    }
}