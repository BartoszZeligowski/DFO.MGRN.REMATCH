using Agresso.Interface.CommonExtension;
using Agresso.Interface.TopGenExtension;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO
{
    public static class ExtensionMethods
    {
        public static void SetColumnToReadOnly(this IField input, int rowIndex, bool setReadOnly)
        {
            if (input == null)
            {
                CurrentContext.Message.Display("Field is null.");
                return;
            }

            if (!input.Section.SupportsUIProperties)
                input.Section.SupportsUIProperties = true; //throw new Exception(String.Format("Section connected to {0} does not supportUIProperties",input.FieldName));

            IDecorator dec = input.Form.Decorator;
            IDecoration decoration = dec.GetCellDecoration(input, rowIndex);
            decoration.ReadOnly = setReadOnly;
            decoration.Apply();
        }

        /// <summary>
        /// GETS the value of a Field
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetValue(this IField input)
        {
            return input.Form.Data.Tables[input.Section.TableName].Rows[0][input.FieldName].ToString();
        }

        /// <summary>
        /// GETS the value of a Field with parameter rowIndex
        /// </summary>
        /// <param name="input"></param>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        public static string GetValue(this IField input, int rowIndex)
        {
            return input.Form.Data.Tables[input.Section.TableName].Rows[rowIndex][input.FieldName].ToString();
        }

        /// <summary>
        /// SETS value to a Field/column
        /// </summary>
        /// <param name="input"></param>
        /// <param name="value"></param>
        public static void SetValue(this IField input, object value)
        {
            input.Form.Data.Tables[input.Section.TableName].Rows[0][input.FieldName] = value;
        }


        /// <summary>
        /// SETS value to a Field/column with a parameter for rowIndex
        /// </summary>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="rowIndex"></param>
        public static void SetValue(this IField input, object value, int rowIndex)
        {
            input.Form.Data.Tables[input.Section.TableName].Rows[rowIndex][input.FieldName] = value;
        }

        public static DataTable GetTable(this ISection sec)
        {
            return sec.Form.Data.Tables[sec.TableName];
        }
    }
}
