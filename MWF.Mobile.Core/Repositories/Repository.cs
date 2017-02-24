using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using MWF.Mobile.Core.Extensions;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Attributes;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.Repositories
{
    public abstract class Repository<T> : IRepository<T> where T : class, IBlueSphereEntity, new()
    {

        protected IDataService _dataService;

        #region Construction

        public Repository(IDataService dataService)
        {
            _dataService = dataService;
        }

        #endregion

        #region IRepository<T> Members

        public virtual Task DeleteAllAsync()
        {
            var connection = _dataService.GetAsyncDBConnection();
            return this.DeleteAllRecursiveAsync(typeof(T), connection);
        }

        public virtual Task DeleteAsync(T entity)
        {
            return _dataService.RunInTransactionAsync(c =>
            {
                this.DeleteRecursive(entity, c);
            });
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            var connection = _dataService.GetAsyncDBConnection();

            var entities = await connection.Table<T>().ToListAsync();

            if (typeof(T).HasChildRelationProperties())
                await this.PopulateChildrenRecursiveAsync(entities, connection);

            return entities;
        }

        public virtual async Task<T> GetByIDAsync(Guid ID)
        {
            T entity = null;

            var connection = _dataService.GetAsyncDBConnection();
            var data = await connection.Table<T>().ToListAsync();

            entity = data.Where(e => e.ID == ID).FirstOrDefault();

            if (entity != null && typeof(T).HasChildRelationProperties())
                await PopulateChildrenRecursiveAsync(entity, connection);

            return entity;
        }

        public virtual void Insert(T entity, Database.IConnection transactionConnection)
        {
            var connection = transactionConnection ?? _dataService.GetDBConnection();

            this.InsertRecursive(entity, connection);
        }

        public virtual Task InsertAsync(T entity)
        {
            return _dataService.RunInTransactionAsync(c =>
            {
                this.InsertRecursive(entity, c);
            });
        }

        public virtual void Insert(IEnumerable<T> entities, Database.IConnection transactionConnection)
        {
            var connection = transactionConnection ?? _dataService.GetDBConnection();

            foreach (var entity in entities)
            {
                this.InsertRecursive(entity, connection);
            }
        }

        public virtual Task InsertAsync(IEnumerable<T> entities)
        {
            return _dataService.RunInTransactionAsync(c =>
            {
                this.Insert(entities, c);
            });
        }

        private void InsertRecursive(IBlueSphereEntity entity, Database.IConnection connection)
        {
            try
            {
                connection.Insert(entity);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to insert into table {0}, entity ID {1}.", entity.GetType().GetTableName(), entity.ID), ex);
            }

            var relationshipProperties = entity.GetType().GetChildRelationProperties();

            foreach (var relationshipProperty in relationshipProperties)
            {
                this.DoRecursiveTreeAction(entity, relationshipProperty, (parent, child) =>
                {
                    SetForeignKeyOnChild(parent, child);
                    this.InsertRecursive(child, connection);
                });
            }
        }

        public virtual Task UpdateAsync(T entity)
        {
            return _dataService.RunInTransactionAsync(c =>
            {
                var data = c.Table<T>().ToList();

                var existingEntity = data.Where(e => e.ID == entity.ID).FirstOrDefault();

                if (existingEntity != null)
                {
                    if (typeof(T).HasChildRelationProperties())
                        this.PopulateChildrenRecursive(existingEntity, c);

                    this.DeleteRecursive(existingEntity, c);
                }

                this.InsertRecursive(entity, c);
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///  Deletes a potentially nested object graph from the appropriate tables using the
        ///  ChildRelationship and ForeignKey attributes to guide the process
        /// </summary>
        /// <param name="entity"></param>
        private void DeleteRecursive(IBlueSphereEntity entity, Database.IConnection connection)
        {
            try
            {
                connection.Delete(entity);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to delete from table {0}, entity ID {1}.", entity.GetType().GetTableName(), entity.ID), ex);
            }

            var relationshipProperties = entity.GetType().GetChildRelationProperties();

            foreach (var relationshipProperty in relationshipProperties)
            {
                this.DoRecursiveTreeAction(entity, relationshipProperty, (parent, child) =>
                {
                    this.DeleteRecursive(child, connection);
                });
            }
        }

        /// <summary>
        /// Deletes all items of the specified type from their respective tables
        /// plus any child types as specified by the ChildRelationship attribute
        /// </summary>
        /// <param name="type"></param>
        private void DeleteAllRecursive(Type type, Database.IConnection connection)
        {
            this.DeleteAllFromTable(type, connection);

            var childTypes = type.GetChildRelationTypes();

            foreach (var childType in childTypes)
            {
                this.DeleteAllRecursive(childType, connection);
            }
        }

        private async Task DeleteAllRecursiveAsync(Type type, Database.IAsyncConnection connection)
        {
            await DeleteAllFromTableAsync(type, connection);

            var childTypes = type.GetChildRelationTypes();

            foreach (var childType in childTypes)
            {
                await this.DeleteAllRecursiveAsync(childType, connection);
            }
        }

        /// <summary>
        /// For the parent entity pulled from a table, populates any children
        /// as labelled with the ChildRelationship. 
        /// </summary>
        /// <param name="parent"></param>
        protected async Task PopulateChildrenRecursiveAsync(IBlueSphereEntity parent, Database.IAsyncConnection connection)
        {
            var relationshipProperties = parent.GetType().GetChildRelationProperties();

            foreach (var relationshipProperty in relationshipProperties)
            {
                Type childType = relationshipProperty.GetTypeOfChildRelation();
                string childIdentifyingPropertyName = relationshipProperty.GetIdentifyingPropertyNameOfChildRelation();
                object childIdentifyingPropertyValue = relationshipProperty.GetIdentifyingPropertyValueOfChildRelation();

                IList children = await this.GetChildrenAsync(parent, childType, childIdentifyingPropertyName, childIdentifyingPropertyValue, connection);

                if (relationshipProperty.GetCardinalityOfChildRelation() == RelationshipCardinality.OneToOne)
                {
					Debug.Assert(children.Count == 1, string.Format("Expected one child of type {0} for parent {1} '{2}' but found {3}", childType.Name, parent.GetType().Name, parent.ID, children.Count));
                    relationshipProperty.SetValue(parent, children[0]);
                }
                else if (relationshipProperty.GetCardinalityOfChildRelation() == RelationshipCardinality.OneToZeroOrOne)
                {
					Debug.Assert(children.Count <= 1, string.Format("Expected zero or one child of type {0} for parent {1} '{2}' but found {3}", childType.Name, parent.GetType().Name, parent.ID, children.Count));

                    if (children.Count == 1)
                        relationshipProperty.SetValue(parent, children[0]);
                }
                else
                {
                    relationshipProperty.SetValue(parent, children);
                }

                foreach (var child in children)
                {
                    await this.PopulateChildrenRecursiveAsync(child as IBlueSphereEntity, connection);
                }
            }
        }

        /// <summary>
        /// Private non-async version of PopulateChildrenRecursiveAsync for use within a transaction
        /// </summary>
        private void PopulateChildrenRecursive(IBlueSphereEntity parent, Database.IConnection connection)
        {
            var relationshipProperties = parent.GetType().GetChildRelationProperties();

            foreach (var relationshipProperty in relationshipProperties)
            {
                Type childType = relationshipProperty.GetTypeOfChildRelation();
                string childIdentifyingPropertyName = relationshipProperty.GetIdentifyingPropertyNameOfChildRelation();
                object childIdentifyingPropertyValue = relationshipProperty.GetIdentifyingPropertyValueOfChildRelation();

                IList children = this.GetChildren(parent, childType, childIdentifyingPropertyName, childIdentifyingPropertyValue, connection);

                if (relationshipProperty.GetCardinalityOfChildRelation() == RelationshipCardinality.OneToOne)
                {
					Debug.Assert(children.Count == 1, string.Format("Expected one child of type {0} for parent {1} '{2}' but found {3}", childType.Name, parent.GetType().Name, parent.ID, children.Count));
                    relationshipProperty.SetValue(parent, children[0]);
                }
                else if (relationshipProperty.GetCardinalityOfChildRelation() == RelationshipCardinality.OneToZeroOrOne)
                {
					Debug.Assert(children.Count <= 1, string.Format("Expected zero or one child of type {0} for parent {1} '{2}' but found {3}", childType.Name, parent.GetType().Name, parent.ID, children.Count));

                    if (children.Count == 1)
                        relationshipProperty.SetValue(parent, children[0]);
                }
                else
                {
                    relationshipProperty.SetValue(parent, children);
                }

                foreach (var child in children)
                {
                    this.PopulateChildrenRecursive(child as IBlueSphereEntity, connection);
                }
            }
        }

        /// <summary>
        /// Overload of PopulateChildrenRecursive that deals with
        /// a collection of parents
        /// </summary>
        /// <param name="parents"></param>
        protected async Task PopulateChildrenRecursiveAsync(IEnumerable parents, Database.IAsyncConnection connection)
        {
            try
            {
                foreach (var parent in parents)
                {
                    await this.PopulateChildrenRecursiveAsync(parent as IBlueSphereEntity, connection);
                }
            }
            catch(ArgumentOutOfRangeException e)
            {

            }
        }

        private async Task<IList> GetChildrenAsync(IBlueSphereEntity parent, Type childType, string childIdentifyingPropertyName, object childIdentifyingPropertyValue, Database.IAsyncConnection connection)
        {
            var tableMapping = await connection.GetMappingAsync(childType);

            string query = string.Format("select * from {0} where {1} = ?", childType.GetTableName(),
                                                                            childType.GetForeignKeyName(parent.GetType()));

            if (!string.IsNullOrEmpty(childIdentifyingPropertyName))
            {
                query = query + string.Format(" AND {0} = ?", childIdentifyingPropertyName);
            }

            IList<object> queryResults;

            if (!string.IsNullOrEmpty(childIdentifyingPropertyName))
            {
                queryResults = await connection.QueryAsync(CancellationToken.None, tableMapping, query, parent.ID, childIdentifyingPropertyValue);
            }
            else
            {
                queryResults = await connection.QueryAsync(CancellationToken.None, tableMapping, query, parent.ID);
            }

            // Create a typed generic list we can assign back to parent element
            IList genericList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(childType));

            foreach (object item in queryResults)
            {
                genericList.Add(item);
            }

            return genericList;
        }

        private IList GetChildren(IBlueSphereEntity parent, Type childType, string childIdentifyingPropertyName, object childIdentifyingPropertyValue, Database.IConnection connection)
        {
            var tableMapping = connection.GetMapping(childType);

            string query = string.Format("select * from {0} where {1} = ?", childType.GetTableName(),
                                                                            childType.GetForeignKeyName(parent.GetType()));

            if (!string.IsNullOrEmpty(childIdentifyingPropertyName))
            {
                query = query + string.Format(" AND {0} = ?", childIdentifyingPropertyName);
            }

            IList<object> queryResults;

            if (!string.IsNullOrEmpty(childIdentifyingPropertyName))
            {
                queryResults = connection.Query(tableMapping, query, parent.ID, childIdentifyingPropertyValue);
            }
            else
            {
                queryResults = connection.Query(tableMapping, query, parent.ID);
            }

            // Create a typed generic list we can assign back to parent element
            IList genericList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(childType));

            foreach (object item in queryResults)
            {
                genericList.Add(item);
            }

            return genericList;
        }

        // Deletes all items from the table associated with the specified type
        private void DeleteAllFromTable(Type type, Database.IConnection connection)
        {
            string command = string.Format("delete from {0}", type.GetTableName());
            connection.Execute(command);
        }

        private async Task DeleteAllFromTableAsync(Type type, Database.IAsyncConnection connection)
        {
            string command = string.Format("delete from {0}", type.GetTableName());
            await connection.ExecuteAsync(command);
        }

        // Ensures the property on the child labelled with the foreign key attribute lines up with
        // the id of the parent
        private static void SetForeignKeyOnChild(IBlueSphereEntity parent, IBlueSphereEntity child)
        {
            PropertyInfo foreignKeyPropInfo = child.GetType().GetForeignKeyProperty(parent.GetType());
            foreignKeyPropInfo.SetValue(child, parent.ID);
        }

        /// <summary>
        /// Performs some action (e.g. insert or delete) recursively on the hierarchical object graph using using the
        ///  ChildRelationship and ForeignKey attributes to guide the process. Deals with the cases where child properties
        ///  are lists or single items.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="relationshipProperty"></param>
        /// <param name="action"></param>
        private void DoRecursiveTreeAction(IBlueSphereEntity entity, PropertyInfo relationshipProperty, Action<IBlueSphereEntity, IBlueSphereEntity> action)
        {

            if (relationshipProperty.GetCardinalityOfChildRelation() == RelationshipCardinality.OneToOne)
            {
                //Child Property is a single Bluesphere entity
                IBlueSphereEntity singleChild = relationshipProperty.GetValue(entity, null) as IBlueSphereEntity;

                if (singleChild == null)
                {
                    throw new ArgumentException(string.Format("{0} type property {1} is not a  BlueSphere entity", entity.GetType().ToString(), relationshipProperty.Name));
                }

                action.Invoke(entity, singleChild);
            }
            else if (relationshipProperty.GetCardinalityOfChildRelation() == RelationshipCardinality.OneToZeroOrOne)
            {
                //Child Property is a single Bluesphere entity but may be null
                var singleChild = relationshipProperty.GetValue(entity, null);

                if (singleChild != null)
                {
                    var singleChildBlueSphereEntity = singleChild as IBlueSphereEntity;

                    if (singleChildBlueSphereEntity == null)
                    {
                        throw new ArgumentException(string.Format("{0} type property {1} is not a  BlueSphere entity", entity.GetType().ToString(), relationshipProperty.Name));
                    }

                    action.Invoke(entity, singleChildBlueSphereEntity);
                }
            }
            else
            {
                // Child property is a collection of Bluesphere entities
                IEnumerable<IBlueSphereEntity> children = relationshipProperty.GetValue(entity, null) as IEnumerable<IBlueSphereEntity>;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        action.Invoke(entity, child);
                    }
                }
            }
        }

        #endregion

    }

}
