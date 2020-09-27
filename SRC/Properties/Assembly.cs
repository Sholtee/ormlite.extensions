/********************************************************************************
* Assembly.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Resources;
using System.Runtime.CompilerServices;

[
assembly:
    NeutralResourcesLanguage("en"),
#if DEBUG
    InternalsVisibleTo("Solti.Utils.OrmLite.Extensions.Tests"),
    InternalsVisibleTo("DynamicProxyGenAssembly2"), // Moq
#endif
    InternalsVisibleTo("Solti.Utils.OrmLite.Extensions.Perf")
]