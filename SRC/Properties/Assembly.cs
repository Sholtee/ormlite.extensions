/********************************************************************************
* Assembly.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Resources;
using System.Runtime.CompilerServices;

[
assembly:
    NeutralResourcesLanguage("en"),
    CLSCompliant(true),
#if DEBUG
    InternalsVisibleTo("Solti.Utils.OrmLite.Extensions.Tests"),
    InternalsVisibleTo("DynamicProxyGenAssembly2"), // Moq
#endif
    InternalsVisibleTo("Solti.Utils.OrmLite.Extensions.Perf")
]