using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api.Filter;

public static class GlobalApiActionManager
{
    private static readonly List<ApiActionFilterAttribute> _filters = new List<ApiActionFilterAttribute>();

    public static void AddFilter(ApiActionFilterAttribute filter)
    {
        _filters.Add(filter);
    }

    public static IEnumerable<ApiActionFilterAttribute> GetFilters()
    {
        return _filters;
    }
}

public static class GlobalBackendApiActionManager
{
    private static readonly List<ApiBackendActionFilterAttribute> _filters = new List<ApiBackendActionFilterAttribute>();

    public static void AddFilter(ApiBackendActionFilterAttribute filter)
    {
        _filters.Add(filter);
    }

    public static IEnumerable<ApiBackendActionFilterAttribute> GetFilters()
    {
        return _filters;
    }
}
