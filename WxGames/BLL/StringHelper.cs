﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BLL
{
    public static class StringHelper
    {
        /// <summary>
        /// 从字符串中提取数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetNumber(this string str)
        {
            string result = "";
            if (str != null && str != string.Empty)
            {
                // 正则表达式剔除非数字字符（不包含小数点.）
                str = Regex.Replace(str, @"[^\d.\d]", "");
                // 如果是数字，则转换为decimal类型
                if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
                {
                    result = str;
                }
            }
            return result;
        }

        public static bool ExitHanZi(this string str)
        {
            if (str != null && str != string.Empty)
            {
                if (Regex.IsMatch(str, @"[\u4e00-\u9fa5]"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
