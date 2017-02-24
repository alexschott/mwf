using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models.Attributes;

namespace MWF.Mobile.Core.Extensions
{
    public static class ReflectionExtensions
    {

        public static List<PropertyInfo> GetChildRelationProperties(this Type type)
        {
            return (from property in type.GetRuntimeProperties()
                    where property.GetCustomAttribute<ChildRelationshipAttribute>() != null
                    select property).ToList();
        }

        public static List<Type> GetChildRelationTypes(this Type type)
        {
            return (from property in type.GetRuntimeProperties()
                    where property.GetCustomAttribute<ChildRelationshipAttribute>() != null
                    select property.GetCustomAttribute<ChildRelationshipAttribute>().ChildType).ToList();
        }

        public static bool HasChildRelationProperties(this Type type)
        {
            return type.GetChildRelationProperties().Any();
        }

        public static List<PropertyInfo> GetForeignKeyProperties(this Type type)
        {
            return (from property in type.GetRuntimeProperties()
                    where property.GetCustomAttribute<ForeignKeyAttribute>() != null
                    select property).ToList();
        } 

        public static Type GetTypeOfChildRelation(this PropertyInfo propertyInfo)
        {
            ChildRelationshipAttribute attr = propertyInfo.GetCustomAttribute<ChildRelationshipAttribute>();

            return (attr == null) ? null : attr.ChildType;
        }

        public static string GetIdentifyingPropertyNameOfChildRelation(this PropertyInfo propertyInfo)
        {
            ChildRelationshipAttribute attr = propertyInfo.GetCustomAttribute<ChildRelationshipAttribute>();

            return (attr == null) ? null : attr.ChildIdentifyingPropertyName;
        }

        public static object GetIdentifyingPropertyValueOfChildRelation(this PropertyInfo propertyInfo)
        {
            ChildRelationshipAttribute attr = propertyInfo.GetCustomAttribute<ChildRelationshipAttribute>();

            return (attr == null) ? null : attr.ChildIdentifyingPropertyValue;
        }

        public static RelationshipCardinality GetCardinalityOfChildRelation(this PropertyInfo propertyInfo)
        {
           ChildRelationshipAttribute attr = propertyInfo.GetCustomAttribute<ChildRelationshipAttribute>();

           Debug.Assert(attr != null);

           return attr.Cardinality;
        }

        public static string GetTableName(this Type type)
        {
            var tableName = type.Name;
            var tableAttribute = type.GetTypeInfo().GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null && tableAttribute.Name != null)
                tableName = tableAttribute.Name;

            return tableName;
        }

        public static string GetColumnName(this PropertyInfo property)
        {
            var column = property.Name;
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttribute != null && columnAttribute.Name != null)
                column = columnAttribute.Name;

            return column;
        }

        public static string GetForeignKeyName(this Type childType, Type parentType)
        {

            return childType.GetForeignKeyProperty(parentType).GetColumnName();

        }

        public static PropertyInfo GetForeignKeyProperty(this Type childType, Type parentType)
        {

            var foreignKeyProperties = (from property in childType.GetRuntimeProperties()
                                        where property.GetCustomAttribute<ForeignKeyAttribute>() != null
                                        && (property.GetCustomAttribute<ForeignKeyAttribute>() as ForeignKeyAttribute).ForeignType == parentType
                                        select property);

            //Contract.Assert(foreignKeyProperties.Any(), string.Format("Type {0} does not contain any foreign key references back to parent type {1}", childType.ToString(), parentType.ToString()));

            return foreignKeyProperties.Single();

        }

    }

}
