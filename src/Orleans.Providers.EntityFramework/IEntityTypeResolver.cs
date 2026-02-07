using System;

namespace Orleans.Providers.EntityFramework
{
    public interface IEntityTypeResolver
    {
        Type ResolveEntityType(Type stateType);
        Type ResolveStateType(Type stateType);
    }
}