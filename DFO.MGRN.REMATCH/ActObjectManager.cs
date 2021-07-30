using Agresso.Interface.CommonExtension;
using Agresso.Interface.TopGenExtension;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace DFO
{
    public class ActObjectManager
    {
        IForm _form;
        public static string REF_TABLENAME = "objman_ref";
        public static string CONTAINER_TABLENAME = "objman_container";

        private ActObjectManager(IForm form)
        {
            _form = form;
        }

        /// <summary>
        /// Must be created in OnCleared() to make this usable!
        /// </summary>
        /// <param name="form"></param>
        public static void Initialize(IForm form, string ref_id, bool eraseIfAlreadyExists = true)
        {
            if (!form.Data.Tables.Contains(REF_TABLENAME))
            {
                DataTable dt = new DataTable()
                {
                    TableName = REF_TABLENAME//"objman_ref"
                };

                dt.Columns.Add(new DataColumn("ref", typeof(object)));
                dt.Columns.Add(new DataColumn("ref_id", typeof(string)));
                //dt.Columns.Add(new DataColumn("data", typeof(object)));

                dt.AcceptChanges();

                form.Data.Tables.Add(dt);


                if (!form.Data.Tables.Contains(CONTAINER_TABLENAME))
                {
                    DataTable objmanContainer = new DataTable()
                    {
                        TableName = CONTAINER_TABLENAME
                    };

                    objmanContainer.Columns.Add(new DataColumn("name", typeof(string)));
                    objmanContainer.Columns.Add(new DataColumn("ref", typeof(object)));
                    objmanContainer.AcceptChanges();

                    form.Data.Tables.Add(objmanContainer);
                }

                //_originalObject = data;
            }
            else
            {
                if (eraseIfAlreadyExists)
                {
                    //form.Data.Tables[REF_TABLENAME].Clear();
                    DataTable dt = form.Data.Tables[REF_TABLENAME];
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string id = dt.Rows[i]["ref_id"].ToString();
                        if (id == ref_id)
                        {
                            dt.Rows.RemoveAt(i);
                            i--;
                        }
                    }
                }

            }
        }

        private static DataRow CreateReferenceObject(IForm form, string ref_id)
        {
            DataTable dt = form.Data.Tables[REF_TABLENAME];
            DataRow newrow = dt.NewRow();
            ActObjectManager manager = new ActObjectManager(form);
            newrow["ref"] = manager;
            newrow["ref_id"] = ref_id;
            dt.Rows.Add(newrow);
            dt.AcceptChanges();
            return newrow;
        }

        public void Add(string name, object data)
        {
            DataTable container = _form.Data.Tables[CONTAINER_TABLENAME];
            //Only add data with unique names
            if (container.Select($"name = '{name}'").ToList().Count == 0)
            {
                DataRow newRow = container.NewRow();
                newRow["name"] = name;
                newRow["ref"] = data;
                container.Rows.Add(newRow);
                container.AcceptChanges();
            }
        }

        /// <summary>
        /// Adds data if it not exists. If it does exist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public void Alter(string name, object data)
        {
            DataTable container = _form.Data.Tables[CONTAINER_TABLENAME];
            List<DataRow> rows = container.Select($"name = '{name}'").ToList();
            if (rows.Count == 0)
                Add(name, data);
            else
            {
                DataRow row = rows[0];
                row["ref"] = data;
                container.AcceptChanges();
            }
        }

        /// <summary>
        /// Removes row by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>true if removed, false otherwise</returns>
        public bool Remove(string name)
        {
            DataTable container = _form.Data.Tables[CONTAINER_TABLENAME];
            List<DataRow> rows = container.Select($"name = '{name}'").ToList();
            if (rows.Count > 0)
            {
                container.Rows.Remove(rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes row by row reference
        /// </summary>
        /// <param name="row"></param>
        /// <returns>true if removed, false otherwise</returns>
        public bool Remove(DataRow row)
        {
            DataTable container = _form.Data.Tables[CONTAINER_TABLENAME];
            if (container.Rows.Contains(row))
            {
                container.Rows.Remove(row);
                return true;
            }

            return false;
        }

        public static ActObjectManager GetManager(IForm form, string ref_id, bool showMessage = true)
        {
            if (form.Data.Tables.Contains(REF_TABLENAME))
            {
                DataTable refTable = form.Data.Tables[REF_TABLENAME];
                DataRow relatedRow = refTable.Select($"ref_id = '{ref_id}'").FirstOrDefault();
                //if (refTable.Rows.Count == 0)
                if (relatedRow == null)
                    relatedRow = CreateReferenceObject(form, ref_id);
                return relatedRow?["ref"] as ActObjectManager;
                //return (refTable.Rows.Count > 0) ? (refTable.Rows[0]["ref"] as ActObjectManager) : null;
            }

            if (showMessage)
                CurrentContext.Message.Display(MessageDisplayType.Error, "ObjectManager has not been initialized! Do this in OnCleared!");

            return null;
        }

        public List<T> GetAllObjectsOfType<T>()
        {
            List<T> objects = new List<T>();
            foreach (DataRow row in _form.Data.Tables[CONTAINER_TABLENAME].Rows)
            {
                if (row["ref"] is T)
                {
                    T value = (T)Convert.ChangeType(row["ref"], typeof(T), CultureInfo.InvariantCulture);
                    objects.Add(value);
                }
            }

            return objects;
        }

        public DataTable GetContainer()
        {
            return _form.Data.Tables[CONTAINER_TABLENAME];
        }

        public T GetValue<T>(string name)
        {
            List<DataRow> relatedRows = _form.Data.Tables[CONTAINER_TABLENAME].Select($"name = '{name}'").ToList();
            object obj = (relatedRows.Count > 0) ? relatedRows[0]["ref"] : null;

            if (obj == null)
                return default(T);

            T value = (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture); //(T) obj;

            return value;
        }
    }
}