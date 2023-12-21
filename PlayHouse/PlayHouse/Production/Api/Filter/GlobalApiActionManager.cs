using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api.Filter;

public static class GlobalApiActionManager
{
    private static readonly List<ApiActionFilterAttribute> _filters = new List<ApiActionFilterAttribute>();
    private static readonly List<ApiBackendActionFilterAttribute> _backendfilters = new List<ApiBackendActionFilterAttribute>();

    public static IEnumerable<ApiActionFilterAttribute>  Filters => _filters;
    public static IEnumerable<ApiBackendActionFilterAttribute> BackendFilters => _backendfilters;

    public static void AddFilter(ApiActionFilterAttribute filter)
    {
        _filters.Add(filter);
    }

    //public static IEnumerable<ApiActionFilterAttribute> Filter()
    //{
    //    return _filters;
    //}

    public static void AddFilter(ApiBackendActionFilterAttribute filter)
    {
        _backendfilters.Add(filter);
    }

    //public static IEnumerable<ApiBackendActionFilterAttribute> GetFilters()
    //{
    //    return _filters;
    //}
}

//public static class GlobalBackendApiActionManager
//{
//    private static readonly List<ApiBackendActionFilterAttribute> _filters = new List<ApiBackendActionFilterAttribute>();

//    public static void AddFilter(ApiBackendActionFilterAttribute filter)
//    {
//        _filters.Add(filter);
//    }

//    public static IEnumerable<ApiBackendActionFilterAttribute> GetFilters()
//    {
//        return _filters;
//    }
//}
