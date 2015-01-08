using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web;

namespace JustConnekt.Models.Utility
{
    public class DatabaseClass
    {

       
        /// <summary>
        /// Will cast an object of certain type T based on dictionary (Refer to mappingattribute)
        /// </summary>
        /// <typeparam name="T">Type you wish to cast to</typeparam>
        /// <param name="dic">Dictionary of PropertyName, Value</param>
        /// <returns>Object of given type</returns>
        public static T CastFromDictionary<T>(Dictionary<string, dynamic> dic) where T : new()
        {

            var type = typeof(T);
            var properties = type.GetProperties();

            T r_val = new T();
            
            foreach(var prop in properties)
            {
                

                var propertyType = prop.PropertyType.Name;
                
                
                var temp = prop.GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();

                if (temp != null && temp.ignore) continue;

                string keyName = getKeyName(prop, temp);

                if(keyName == "crazyAdventure")
                {

                    
                }

                PropertyInfo propInfo = type.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                if (null != propInfo && propInfo.CanWrite)
                {
                    
                    if (!dic.Keys.Any(m => m.Equals(keyName, StringComparison.InvariantCultureIgnoreCase))) continue;
                        propInfo.SetValue(r_val, dic[keyName]);
                }

            }

            return r_val;
        }



        public static List<T> FindObject<T>(string condition, string table) where T : new()
        {
            var foundSqlObj = ExecuteSqlTransaction(null, "Select * from " + table + " where " + condition + ";", null);
            List<T> r_val = new List<T>();

            foreach(var row in foundSqlObj)
            {
                r_val.Add(CastFromDictionary<T>(row));
            }
            return r_val;
        }


        /// <summary>
        /// Creates a dictionary based on object and its properties (Refer to mappingattribute)
        /// </summary>
        /// <typeparam name="T">The given type of argument</typeparam>
        /// <param name="arg">Object you wish to conver to dic</param>
        /// <returns>Dictionary of properties you wish mapped to</returns>
        public static Dictionary<string, dynamic> getAllProperties<T>(T arg) where T : new()
        {
            var type = arg.GetType();

            Dictionary<string, dynamic> r_val = new Dictionary<string, dynamic>();

            foreach(var prop in type.GetProperties())
            {
                var temp = prop.GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();
                if (temp != null && temp.ignore) continue;

                

                string keyName = getKeyName(prop, temp);


                var valueOfProp = type.GetProperty(prop.Name).GetValue(arg);
                

                r_val.Add(keyName, (valueOfProp == null) ? null : valueOfProp);
            }

            return r_val;
        }

        /// <summary>
        /// Get's dictionary of [table, relationship]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, dynamic>> getAllProperties<T>(T arg, bool MapBoth) where T : new()
        {
            var type = arg.GetType();

            Dictionary<string, dynamic> r_val = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> r_val2 = new Dictionary<string, dynamic>();

            foreach (var prop in type.GetProperties())
            {
                var temp = prop.GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();
                if (temp != null && temp.ignore) continue;



                string keyName = getKeyName(prop, temp);


                var valueOfProp = type.GetProperty(prop.Name).GetValue(arg);

                if(temp != null && (temp.sql_Rel || temp.sql_RelationshipKey))
                    r_val2.Add(keyName, (valueOfProp == null) ? null : valueOfProp);

                if((temp != null && (!temp.sql_Rel || temp.sql_RelationshipKey)) || temp == null)
                    r_val.Add(keyName, (valueOfProp == null) ? null : valueOfProp);
            }

            return new Dictionary<string, Dictionary<string, dynamic>>() { 
                {"table", r_val}
            , {"relationship", r_val2 }};
        }


       

        public static void UpdateFullQuery<T>(T arg, string condition) where T : new()
        {

            MappingAttribute temp = arg.GetType().GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();

            var RelTable = getAllProperties<T>(arg, true);
            UpdateQuery(RelTable["dictionary"], temp.sql_RelationTable, condition);
            UpdateQuery(RelTable["table"], temp.sql_ClassTable, condition);

        }
        public static void UpdateQuery(dynamic arg, string table, string condition)
        {
            var dic = ConstructUpdateQuery(arg, table, condition);
            ExecuteSqlTransaction(null, dic["string"], dic["dictionary"], true);
        }

        public static void UpdateQuery(Dictionary<string, dynamic> arg, string table, string condition)
        {
            var dic = ConstructUpdateQuery(arg, table, condition);
            ExecuteSqlTransaction(null, dic["string"], dic["dictionary"], true);
        }

        public static Dictionary<string, dynamic> ConstructUpdateQuery(dynamic arg, string table, string condition)
        {
            var newDic = getAllProperties(arg);
            
            return ConstructUpdateQuery(newDic , table, condition);
        }

        /// <summary>
        /// Creates an update query for your database
        /// </summary>
        /// <param name="arg">Values you wish to add</param>
        /// <param name="table">Table name</param>
        /// <param name="condition">Row(s) you wish to modify</param>
        /// <returns>a dictionary with "string" and "dictionary"</returns>
        public static Dictionary<string, dynamic> ConstructUpdateQuery(Dictionary<string, dynamic> arg, string table, string condition)
        {

            Dictionary<string, dynamic> dyc_RVal = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> innerDyc = new Dictionary<string, dynamic>();

            string r_val = "Update " + table + " Set ";
            bool flag = false;

            foreach(var prop in arg)
            {
                if (prop.Value == null || prop.Value.ToString() == "") continue;

                innerDyc.Add(prop.Key, prop.Value);
                r_val += ((flag) ? ", " : "") + prop.Key+ " = " + "@" + prop.Key;
                flag = true;
            }
            r_val += " where " + condition + ";";

            dyc_RVal.Add("string", r_val);
            dyc_RVal.Add("dictionary", innerDyc);

            return dyc_RVal;
        }

        public static Dictionary<string, dynamic> getRelTable<T> (T arg) where T : new()
        {
            var type = arg.GetType();

            Dictionary<string, dynamic> r_val = new Dictionary<string, dynamic>();

            foreach (var prop in type.GetProperties())
            {
                var temp = prop.GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();
                if (temp == null || !temp.sql_Rel) continue;



                string keyName = getKeyName(prop, temp);


                var valueOfProp = type.GetProperty(prop.Name).GetValue(arg);


                r_val.Add(keyName, (valueOfProp == null) ? null : valueOfProp);
            }

            return r_val;
        }


        public static void InsertRelationship<T>(T arg) where T : new()
        {
            var temp = arg.GetType().GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();
            
            InsertRelationship<T>(arg, temp.sql_ClassTable, temp.sql_RelationTable);
        }
        /// <summary>
        /// Used to insert an object's relationship + actual
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        public static void InsertRelationship<T>(T arg, string table, string relationTable) where T : new()
        {
            var RelTable = getAllProperties<T>(arg, true);
            InsertQuery(RelTable["relationship"], relationTable);
            InsertQuery(RelTable["table"], table);
        }

        public static void InsertQuery(Dictionary<string, dynamic> arg, string table)
        {
            
            var dic = ConstructInsertQuery(arg, table);
            ExecuteSqlTransaction(null, dic["string"], dic["dictionary"], true);
        }


        public static void InsertQuery(dynamic arg, string table)
        {
            var dic = ConstructInsertQuery(arg, table);
            ExecuteSqlTransaction(null, dic["string"], dic["dictionary"], true);
        }

        public static Dictionary<string, dynamic> ConstructInsertQuery(dynamic arg, string table)
        {
            var newdic = getAllProperties(arg);
            return ConstructInsertQuery(newdic, table);
        }
        /// <summary>
        /// Creates an insert query
        /// </summary>
        /// <param name="arg">Dictionary of values</param>
        /// <param name="table">Table of choice</param>
        /// <returns>a dictionary with "string" and "dictionary"</returns>
        public static Dictionary<string, dynamic> ConstructInsertQuery(Dictionary<string, dynamic> arg, string table)
        {

            Dictionary<string, dynamic> dyc_RVal = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> innerDyc = new Dictionary<string, dynamic>();

            string r_val = "insert into " + table + " ( ";
            string values = "values (";

            bool flag = false;
            foreach(var entry in arg)
            {
                
                if(entry.Value == null || entry.Value.ToString() == "") continue;
                innerDyc.Add(entry.Key, entry.Value);
                
                r_val += ((flag) ? ", " : "") +  entry.Key;
                values += ((flag) ? ", " : "") + "@" + entry.Key;
                flag = true;
            }

            r_val += ") " + values + ");";

            dyc_RVal.Add("string", r_val);
            dyc_RVal.Add("dictionary", innerDyc);

            return dyc_RVal;
            #region old version
            /* var type = arg.GetType();

            var r_val = "insert into " + table + " (";
            string values = "(";
            foreach(var prop in type.GetProperties())
            {

                var temp = prop.GetCustomAttributes(typeof(MappingAttribute), false).OfType<MappingAttribute>().SingleOrDefault();
                
                if (temp != null && temp.ignore) continue;
                var propVal = prop.GetValue(arg);
                if (propVal == null || propVal.ToString() == "")
                    continue;
                 string keyName = getKeyName(prop, temp);

                var typeOfProperty = prop.PropertyType.Name;

                

                r_val += keyName + ", ";
                values += ((typeOfProperty == "String") ? "'" + propVal + "'" : propVal) + ", ";
                
            }
            r_val += ") values " + values + ");";

            return r_val; */
#endregion
        }


        /// <summary>
        /// Use this if you wish to get the string you should use for a name for a property relating to its SQL <-> Model name between mappingattribute & its name
        /// </summary>
        /// <param name="prop">Property</param>
        /// <param name="temp">Value of mapping attribute for property</param>
        /// <returns>Desired name</returns>
        public static string getKeyName(dynamic prop, dynamic temp)
        {

            string keyName = "";

            if (temp != null && temp.sql_name != null && temp.sql_name != "")
                keyName = temp.sql_name;
            else
                keyName = prop.Name;

            return keyName;
        }


        public static List<Dictionary<string, dynamic>> ExecuteSqlTransaction<T>(string connectionString, string query, T param, bool write) where T : new()
        {
            return ExecuteSqlTransaction(connectionString, query, getAllProperties<T>(param), write);
        }

        public static List<Dictionary<string, dynamic>> ExecuteSqlTransaction(string connectionString, string query, string key, dynamic param, bool write)
        {
            Dictionary<string, dynamic> send_val = new Dictionary<string, dynamic>();
            send_val.Add(key, param);
            return ExecuteSqlTransaction(connectionString, query, send_val, write);
        }



        public static int? CountRows(string connectionString, string query, Dictionary<string, dynamic> param)
        {




            if (connectionString == null || connectionString.IndexOf("!") == 0)
                connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[(connectionString == null) ? "DefaultConnection" : connectionString.Substring(1)].ConnectionString;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                connection.Open();
                int? r_val = null;
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;

                // Start a local transaction.
                transaction = connection.BeginTransaction("SampleTransaction");

                // Must assign both transaction object and connection 
                // to Command object for a pending local transaction
                command.Connection = connection;
                command.Transaction = transaction;


                try
                {
                    command.CommandText = query;

                    foreach (var prop in param)
                    {
                        command.Parameters.Add("@" + prop.Key, prop.Value);
                    }

                    
                    

                        r_val= command.ExecuteNonQuery();
                    
                    

                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                    Console.WriteLine("  Message: {0}", ex.Message);

                    // Attempt to roll back the transaction. 
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        // This catch block will handle any errors that may have occurred 
                        // on the server that would cause the rollback to fail, such as 
                        // a closed connection.
                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                        Console.WriteLine("  Message: {0}", ex2.Message);
                    }

                }
                return r_val;
            }//using


        }//method


        public static List<Dictionary<string, dynamic>> ExecuteSqlTransaction(string connectionString, string query, Dictionary<string, dynamic> param, bool write)
        {




            if (connectionString == null || connectionString.IndexOf("!") == 0)
                connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[(connectionString == null) ? "DefaultConnection" : connectionString.Substring(1)].ConnectionString;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                connection.Open();
                List<Dictionary<string, dynamic>> r_val = new List<Dictionary<string, dynamic>>();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;

                // Start a local transaction.
                transaction = connection.BeginTransaction("SampleTransaction");

                // Must assign both transaction object and connection 
                // to Command object for a pending local transaction
                command.Connection = connection;
                command.Transaction = transaction;


                try
                {
                    command.CommandText = query;

                    foreach (var prop in param)
                    {
                        command.Parameters.Add("@" + prop.Key, prop.Value);
                    }

                    if (write)
                    {
                        
                        command.ExecuteNonQuery();
                    }
                    else
                    {



                        SqlDataReader reader = command.ExecuteReader();



                        while (reader.Read())
                        {

                            r_val.Add(createItem((IDataRecord)reader)); //create item, save item to list
                        }



                        // Attempt to commit the transaction.
                        reader.Close();

                    }

                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                    Console.WriteLine("  Message: {0}", ex.Message);

                    // Attempt to roll back the transaction. 
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        // This catch block will handle any errors that may have occurred 
                        // on the server that would cause the rollback to fail, such as 
                        // a closed connection.
                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                        Console.WriteLine("  Message: {0}", ex2.Message);
                    }

                }
                return r_val;
            }//using


        }//method



        /// <summary>
        /// Will take a connection/query and execute it, taking any results into a list of 
        /// columns + values
        /// </summary>
        /// <param name="connectionString">Pass null if you wish to use default connection, or prepend !(connectionStringName) or pass actual string.</param>
        /// <param name="query">Will execute this SQL query and return results as a list of strings...</param>
        /// <param name="param">Reading call?? null  // Dictionary of params</param>
        /// <returns></returns>
        public static List<Dictionary<string, dynamic>> ExecuteSqlTransaction(string connectionString, string query, Dictionary<string, dynamic> param)
        {


            

            if (connectionString == null || connectionString.IndexOf("!") == 0)
                connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[(connectionString == null) ? "DefaultConnection" : connectionString.Substring(1)].ConnectionString;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                connection.Open();
                List<Dictionary<string, dynamic>> r_val = new List<Dictionary<string, dynamic>>();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;

                // Start a local transaction.
                transaction = connection.BeginTransaction("SampleTransaction");

                // Must assign both transaction object and connection 
                // to Command object for a pending local transaction
                command.Connection = connection;
                command.Transaction = transaction;


                try
                {
                    command.CommandText = query;

                    if(param != null)
                    {
                        foreach(var prop in param)
                        {
                            command.Parameters.Add("@" + prop.Key, prop.Value);
                        }
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlDataReader reader = command.ExecuteReader();



                        while (reader.Read())
                        {

                            r_val.Add(createItem((IDataRecord)reader)); //create item, save item to list
                        }



                        // Attempt to commit the transaction.
                        reader.Close();
                        
                    }

                    transaction.Commit();
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                    Console.WriteLine("  Message: {0}", ex.Message);

                    // Attempt to roll back the transaction. 
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        // This catch block will handle any errors that may have occurred 
                        // on the server that would cause the rollback to fail, such as 
                        // a closed connection.
                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                        Console.WriteLine("  Message: {0}", ex2.Message);
                    }
                    
                }
                return r_val;
            }//using


        }//method


        /// <summary>
        /// Will return a dictionary<string, string> relationship of name of column to value in row.
        /// </summary>
        /// <param name="record">Pass the idatarecord which holds the data for a row</param>
        /// <returns>Returns a dictionary<string, string></returns>
        public static Dictionary<string, dynamic> createItem(IDataRecord record)
        {
            Dictionary<string, dynamic> r_val = new Dictionary<string, dynamic>();


            for (int a = 0; a < record.FieldCount; a++) //iterate through columns of sent row.
            {
                var holder = record[a];
                var holderType = holder.GetType();

                r_val[record.GetName(a)] = (holderType.Name == "DBNull") ? null : holder; //(holderType.Name == "Int32") ? holder : (holderType.Name == "DBNull") ? null : holder.ToString();

            }

            return r_val;
        }

    }
}

