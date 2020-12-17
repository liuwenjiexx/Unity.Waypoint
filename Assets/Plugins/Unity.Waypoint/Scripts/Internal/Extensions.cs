using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngine.Extensions
{
    internal static class Extensions  
    {
        public static IEnumerable<Assembly> Referenced(this IEnumerable<Assembly> assemblies, IEnumerable<Assembly> referenced)
        {

            foreach (var ass in assemblies)
            {
                if (referenced.Where(o => o == ass).FirstOrDefault() != null)
                {
                    yield return ass;
                }
                else
                {
                    foreach (var refAss in ass.GetReferencedAssemblies())
                    {
                        if (referenced.Where(o => o.FullName == refAss.FullName).FirstOrDefault() != null)
                        {
                            yield return ass;
                            break;
                        }
                    }
                }
            }
        }
    }
}