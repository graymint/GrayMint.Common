using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable UnusedMember.Global
namespace GrayMint.Common.EntityFrameworkCore;

public static class EfCoreExtensions
{
    extension<TProperty>(PropertyBuilder<TProperty> propertyBuilder)
    {
        /// <summary>
        /// Sets a default value on a property. The constraint name is used by SQL Server for named default constraints
        /// and is ignored by PostgreSQL and other providers that do not support named default constraints.
        /// </summary>
        public PropertyBuilder<TProperty> HasDefaultValueWithConstraintName(object? defaultValue, string constraintName)
        {
            propertyBuilder.HasDefaultValue(defaultValue);
            // "Relational:DefaultConstraintName" is the annotation key read by the SQL Server provider at runtime.
            // Other providers ignore unknown annotations, so no SQL Server NuGet reference is required.
            propertyBuilder.HasAnnotation("Relational:DefaultConstraintName", constraintName);
            return propertyBuilder;
        }
    }
}
