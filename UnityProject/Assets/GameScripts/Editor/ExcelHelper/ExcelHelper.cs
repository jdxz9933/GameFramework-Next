using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using OfficeOpenXml;
using Sirenix.Utilities;

namespace Game.Editor
{
    public class ExcelIndexAttribute : Attribute
    {
        private int index;

        public int Index => index;

        public ExcelIndexAttribute(int index)
        {
            this.index = index;
        }
    }

    public class ExcelMainAttribute : Attribute
    {
    }

    /// <summary>
    /// 表格竖向List属性
    /// </summary>
    public class ExcelListAttribute : Attribute
    {
    }

    public class ExcelHelper
    {
        public static List<T> ReadExcelData<T>(string excelPath) where T : new()
        {
            if (!File.Exists(excelPath)) return null;
            List<T> result = new List<T>();

            bool isHasList = false;
            int varRowIndex = -1;
            int listVarRowIndex = -1;
            int dataStarIndex = -1;

            PropertyInfo mainPropertyInfo = null;
            PropertyInfo listPropertyInfo = null;
            using (var package = new ExcelPackage(excelPath))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var worksheet = package.Workbook.Worksheets[0];
                var properties = typeof(T).GetProperties();
                Dictionary<PropertyInfo, int> PropertyInfoMap = new Dictionary<PropertyInfo, int>();
                Dictionary<PropertyInfo, int> ListPropertyInfoMap = new Dictionary<PropertyInfo, int>();
                //查找表头
                for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                {
                    var cellValue = worksheet.Cells[row, 1].Value;
                    if (cellValue == null)
                    {
                        dataStarIndex = row;
                        break;
                    }
                    else
                    {
                        string val = cellValue.ToString();
                        if (varRowIndex == -1)
                        {
                            if (string.Equals(val.ToLower(), "##var"))
                            {
                                varRowIndex = row;
                            }
                        }
                        else
                        {
                            if (string.Equals(val.ToLower(), "##var"))
                            {
                                isHasList = true;
                                listVarRowIndex = row;
                            }
                        }
                    }
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    var propertie = properties[i];
                    var excelIndex = propertie.GetAttribute<ExcelIndexAttribute>();
                    if (excelIndex != null)
                    {
                        PropertyInfoMap.Add(propertie, excelIndex.Index);
                    }

                    var excelMain = propertie.GetAttribute<ExcelMainAttribute>();
                    if (excelMain != null)
                    {
                        mainPropertyInfo = propertie;
                    }

                    //如果propertie是否是list属性
                    if (propertie.PropertyType.IsGenericType &&
                        propertie.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        listPropertyInfo = propertie;
                    }
                }

                if (listPropertyInfo == null)
                {
                    isHasList = false;
                }

                //分析对对应的表格参数
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[varRowIndex, col].Value;
                    if (cellValue != null)
                        for (int i = 0; i < properties.Length; i++)
                        {
                            var propertie = properties[i];
                            if (propertie.Name == cellValue.ToString())
                            {
                                if (!PropertyInfoMap.ContainsKey(propertie))
                                    PropertyInfoMap.Add(propertie, col);
                            }
                        }

                    if (isHasList)
                    {
                        var cellValueList = worksheet.Cells[listVarRowIndex, col].Value;
                        if (cellValueList != null)
                        {
                            var objType12 = listPropertyInfo.PropertyType.GetGenericArguments()[0];
                            var objProperties = objType12.GetProperties();
                            for (int i = 0; i < objProperties.Length; i++)
                            {
                                var propertie = objProperties[i];
                                if (propertie.Name == cellValueList.ToString())
                                {
                                    if (!ListPropertyInfoMap.ContainsKey(propertie))
                                        ListPropertyInfoMap.Add(propertie, col);
                                }
                            }
                        }
                    }
                }

                T beCacheItem = default;

                for (int row = dataStarIndex; row <= worksheet.Dimension.Rows; row++)
                {
                    bool isHasMainVal = true;
                    if (mainPropertyInfo != null)
                    {
                        //如果是主属性，判断是否为空
                        var cellValue = worksheet.Cells[row, PropertyInfoMap[mainPropertyInfo]].Value;
                        if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
                        {
                            isHasMainVal = false;
                        }
                    }

                    if (isHasMainVal)
                    {
                        T item = new T();
                        beCacheItem = item;
                        ConvertData(PropertyInfoMap, worksheet, row, ref item);
                        result.Add(item);
                        if (isHasList)
                        {
                            var type = listPropertyInfo.PropertyType;
                            //根据type创建实例
                            var listInstance = Activator.CreateInstance(type);
                            //item里面创建list
                            listPropertyInfo.SetValue(item, listInstance);
                            var list = listPropertyInfo.GetValue(item) as IList;
                            if (list != null)
                            {
                                var objType = listPropertyInfo.PropertyType.GetGenericArguments()[0];
                                var obj = Activator.CreateInstance(objType);
                                ConvertData(ListPropertyInfoMap, worksheet, row, ref obj);
                                list.Add(obj);
                            }
                        }
                    }
                    else
                    {
                        if (beCacheItem != null)
                        {
                            var list = listPropertyInfo.GetValue(beCacheItem) as IList;
                            if (list != null)
                            {
                                var objType = listPropertyInfo.PropertyType.GetGenericArguments()[0];
                                var obj = Activator.CreateInstance(objType);
                                ConvertData(ListPropertyInfoMap, worksheet, row, ref obj);
                                list.Add(obj);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 转换数据
        /// </summary>
        /// <param name="propertyInfoMap"></param>
        /// <param name="worksheet"></param>
        /// <param name="row"></param>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        private static void ConvertData<T>(Dictionary<PropertyInfo, int> propertyInfoMap, ExcelWorksheet worksheet,
            int row, ref T item)
        {
            foreach (var keyValue in propertyInfoMap)
            {
                var index = keyValue.Value;
                var property = keyValue.Key;
                // Debug.Log("row" + row + " " + property.Name + " " + index + " " + property.PropertyType);
                var cellValue = worksheet.Cells[row, index].Value;
                if (property.PropertyType == typeof(int))
                {
                    if (cellValue != null && int.TryParse(cellValue.ToString(), out int val))
                    {
                        property.SetValue(item, val);
                    }
                    else
                    {
                        property.SetValue(item, 0);
                    }
                }
                else if (property.PropertyType == typeof(string))
                {
                    if (cellValue != null)
                        property.SetValue(item, cellValue.ToString());
                }
                else if (property.PropertyType == typeof(bool))
                {
                    if (cellValue != null && bool.TryParse(cellValue.ToString(), out var val))
                    {
                        property.SetValue(item, val);
                    }
                    else
                    {
                        property.SetValue(item, false);
                    }
                }
                else if (property.PropertyType == typeof(long))
                {
                    if (cellValue != null && long.TryParse(cellValue.ToString(), out long val))
                    {
                        property.SetValue(item, val);
                    }
                }
                else if (property.PropertyType == typeof(float))
                {
                    if (cellValue != null && float.TryParse(cellValue.ToString(), out float val))
                    {
                        property.SetValue(item, val);
                    }
                }
            }
        }

        public static void WriteExcelData<T>(string excelPath, List<T> dataList)
        {
            if (!File.Exists(excelPath))
                return;
            // int varRowIndex = -1;
            // int dataStarIndex = -1;
            bool isHasList = false;
            int varRowIndex = -1;
            int listVarRowIndex = -1;
            int dataStarIndex = -1;
            PropertyInfo mainPropertyInfo = null;
            PropertyInfo listPropertyInfo = null;
            using (var package = new ExcelPackage(excelPath))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var worksheet = package.Workbook.Worksheets[0];
                var properties = typeof(T).GetProperties();
                Dictionary<PropertyInfo, int> PropertyInfoMap = new Dictionary<PropertyInfo, int>();
                Dictionary<PropertyInfo, int> ListPropertyInfoMap = new Dictionary<PropertyInfo, int>();

                //查找表头
                for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                {
                    var cellValue = worksheet.Cells[row, 1].Value;
                    if (cellValue == null)
                    {
                        dataStarIndex = row;
                        break;
                    }
                    else
                    {
                        string val = cellValue.ToString();
                        if (varRowIndex == -1)
                        {
                            if (string.Equals(val.ToLower(), "##var"))
                            {
                                varRowIndex = row;
                            }
                        }
                        else
                        {
                            if (string.Equals(val.ToLower(), "##var"))
                            {
                                isHasList = true;
                                listVarRowIndex = row;
                            }
                        }
                    }
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    var propertie = properties[i];
                    var excelIndex = propertie.GetAttribute<ExcelIndexAttribute>();
                    if (excelIndex != null)
                    {
                        PropertyInfoMap.Add(propertie, excelIndex.Index);
                    }

                    var excelMain = propertie.GetAttribute<ExcelMainAttribute>();
                    if (excelMain != null)
                    {
                        mainPropertyInfo = propertie;
                    }

                    //如果propertie是否是list属性
                    if (propertie.PropertyType.IsGenericType &&
                        propertie.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        listPropertyInfo = propertie;
                    }
                }

                if (listPropertyInfo == null)
                {
                    isHasList = false;
                }

                //分析对对应的表格参数
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[varRowIndex, col].Value;
                    if (cellValue != null)
                        for (int i = 0; i < properties.Length; i++)
                        {
                            var propertie = properties[i];
                            if (propertie.Name == cellValue.ToString())
                            {
                                if (!PropertyInfoMap.ContainsKey(propertie))
                                    PropertyInfoMap.Add(propertie, col);
                            }
                        }

                    if (isHasList)
                    {
                        var cellValueList = worksheet.Cells[listVarRowIndex, col].Value;
                        if (cellValueList != null)
                        {
                            var objType12 = listPropertyInfo.PropertyType.GetGenericArguments()[0];
                            var objProperties = objType12.GetProperties();
                            for (int i = 0; i < objProperties.Length; i++)
                            {
                                var propertie = objProperties[i];
                                if (propertie.Name == cellValueList.ToString())
                                {
                                    if (!ListPropertyInfoMap.ContainsKey(propertie))
                                        ListPropertyInfoMap.Add(propertie, col);
                                }
                            }
                        }
                    }
                }

                // //分析对对应的表格参数
                // for (int col = 1; col <= worksheet.Dimension.Columns; col++) {
                //     var cellValue = worksheet.Cells[varRowIndex, col].Value;
                //     if (cellValue != null)
                //         for (int i = 0; i < properties.Length; i++) {
                //             var propertie = properties[i];
                //             if (propertie.Name == cellValue.ToString()) {
                //                 if (!PropertyInfoMap.ContainsKey(propertie))
                //                     PropertyInfoMap.Add(propertie, col);
                //             }
                //         }
                // }

                int startRow = dataStarIndex;
                for (int i = 0; i < dataList.Count; i++)
                {
                    var data = dataList[i];
                    int row = startRow;
                    foreach (var keyValue in PropertyInfoMap)
                    {
                        var index = keyValue.Value;
                        var property = keyValue.Key;
                        var value = property.GetValue(data);
                        if (value != null)
                            worksheet.Cells[row, index].Value = value.ToString();
                    }

                    //查找List属性遍历
                    if (isHasList)
                    {
                        var list = listPropertyInfo.GetValue(data) as IList;
                        if (list != null)
                        {
                            for (int j = 0; j < list.Count; j++)
                            {
                                var obj = list[j];
                                int listRow = startRow;
                                foreach (var keyValue in ListPropertyInfoMap)
                                {
                                    var index = keyValue.Value;
                                    var property = keyValue.Key;
                                    var value = property.GetValue(obj);
                                    if (value != null)
                                        worksheet.Cells[listRow, index].Value = value.ToString();
                                }

                                startRow++;
                            }
                        }
                    }
                }

                package.Save();
            }
        }

        public static void OverrideExcelData<T>(string excelPath, List<T> dataList)
        {
            if (!File.Exists(excelPath))
                return;
            int varRowIndex = -1;
            int dataStarIndex = -1;
            using (var package = new ExcelPackage(excelPath))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var worksheet = package.Workbook.Worksheets[0];
                var properties = typeof(T).GetProperties();
                Dictionary<PropertyInfo, int> PropertyInfoMap = new Dictionary<PropertyInfo, int>();
                //查找表头
                for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                {
                    var cellValue = worksheet.Cells[row, 1].Value;
                    if (cellValue == null)
                    {
                        dataStarIndex = row;
                        break;
                    }

                    string val = cellValue.ToString();
                    if (string.Equals(val.ToLower(), "##var"))
                    {
                        varRowIndex = row;
                    }
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    var propertie = properties[i];
                    var excelIndex = propertie.GetAttribute<ExcelIndexAttribute>();
                    if (excelIndex != null)
                    {
                        PropertyInfoMap.Add(propertie, excelIndex.Index);
                    }
                }

                //分析对对应的表格参数
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[varRowIndex, col].Value;
                    if (cellValue != null)
                        for (int i = 0; i < properties.Length; i++)
                        {
                            var propertie = properties[i];
                            if (propertie.Name == cellValue.ToString())
                            {
                                if (!PropertyInfoMap.ContainsKey(propertie))
                                    PropertyInfoMap.Add(propertie, col);
                            }
                        }
                }

                for (int i = 0; i < dataList.Count; i++)
                {
                    var data = dataList[i];
                    int row = dataStarIndex + i;
                    foreach (var keyValue in PropertyInfoMap)
                    {
                        var index = keyValue.Value;
                        var property = keyValue.Key;
                        var value = property.GetValue(data);
                        if (value != null)
                            worksheet.Cells[row, index].Value = value.ToString();
                    }
                }

                for (var i = dataList.Count; i < 999; i++)
                {
                    var row = dataStarIndex + i;
                    foreach (var kv in PropertyInfoMap)
                    {
                        worksheet.Cells[row, kv.Value].Clear();
                    }
                }

                package.Save();
            }
        }

        public static void WriteExcelData(string excelPath, string idName, string propertyName, string value)
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;

            using (var p = new ExcelPackage(excelPath))
            {
                int index = 0;
                var ws = p.Workbook.Worksheets[0];
                int row = 0;
                foreach (var rowNum in ws.Rows)
                {
                    if ((string)ws.Cells[row, 1].Value == idName)
                    {
                        break;
                    }

                    row++;
                }

                int col = 0;
                foreach (var colNum in ws.Columns)
                {
                    if ((string)ws.Cells[1, col].Value == propertyName)
                    {
                        break;
                    }

                    col++;
                }

                ws.Cells[row, col].Value = value;

                p.Save();
                Debug.Log("Gen png pos success");
            }
        }
    }
}