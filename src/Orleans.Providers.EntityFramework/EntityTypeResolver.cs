using System;

namespace Orleans.Providers.EntityFramework
{
    public class EntityTypeResolver : IEntityTypeResolver
    {
        public virtual Type ResolveEntityType(Type stateType)
        {
            return ResolveStateType(stateType);
        }

        public virtual Type ResolveStateType(Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));

            return stateType;
        }
    }
}