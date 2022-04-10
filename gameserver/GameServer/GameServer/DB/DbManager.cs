using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

public class DbManager
{
     public static MySqlConnection mysql;
     private static JavaScriptSerializer Js = new JavaScriptSerializer();
     public static bool Connect(string db,string ip,int port,string user,string pw)
     {
          mysql = new MySqlConnection();

          string s = $"Database={db};Data Source={ip};port={port};User Id={user};Password={pw}";
          mysql.ConnectionString = s;

          try
          {
               mysql.Open();
               return true;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               throw;
          }
     }

     //是否存在该用户
     public static bool IsAccountExist(string id)
     {
          if (!isSafeSring(id))
          {
               return false;
          }

          string s = $"select * from account where id='{id}'";
          try
          {
               MySqlCommand cmd = new MySqlCommand(s, mysql);
               MySqlDataReader dataReader = cmd.ExecuteReader();
               bool hasRows = dataReader.HasRows;
               dataReader.Close();
               return !hasRows;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               return false;
          }
     }

     public static bool Register(string id,string pw)
     {
          if (!DbManager.isSafeSring(id))
          {
               return false;
          }

          if (!DbManager.isSafeSring(pw))
          {
               return false;
          }

          if (!DbManager.IsAccountExist(id))
          {
               return false;
          }

          string sql = $"insert into account set id='{id}',pw='{pw}'";

          try
          {
               MySqlCommand cmd = new MySqlCommand(sql, mysql);
               cmd.ExecuteNonQuery();
               return true;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               return false;
          }
     }

     public static bool CreatePlayer(string id)
     {
          if (!DbManager.isSafeSring(id))
          {
               return false;
          }

          PlayerData playerData = new PlayerData();
          string data = Js.Serialize(playerData);

          string sql = $"insert into player set id='id',data='{data}'";
          try
          {
               MySqlCommand cmd = new MySqlCommand(sql, mysql);
               cmd.ExecuteNonQuery();
               return true;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               throw;
          }
     }
     
     /// <summary>
     /// 检查用户名和密码
     /// </summary>
     /// <param name="id"></param>
     /// <param name="pw"></param>
     /// <returns></returns>
     public static bool CheckPassword(string id,string pw)
     {
          if (!DbManager.isSafeSring(id))
          {
               return false;
          }

          if (!DbManager.isSafeSring(pw))
          {
               return false;
          }

          string sql = $"select * from account where id='{id}' and pw='{pw}'";
          try
          {
               MySqlCommand cmd = new MySqlCommand(sql,mysql);
               MySqlDataReader dataReader = cmd.ExecuteReader();
               bool hasRows = dataReader.HasRows;
               dataReader.Close();
               return hasRows;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               return false;
          }
     }
     
     /// <summary>
     /// 获取玩家数据
     /// </summary>
     /// <param name="id"></param>
     /// <returns></returns>
     public static PlayerData GetPlayerData(string id)
     {
          if (!DbManager.isSafeSring(id))
          {
               return null;
          }

          string sql = $"select * from player where id ='{id}'";
          try
          {
               MySqlCommand cmd = new MySqlCommand(sql, mysql);
               MySqlDataReader dataReader = cmd.ExecuteReader();
               if (!dataReader.HasRows)
               {
                    dataReader.Close();
                    return null;
               }

               dataReader.Read();
               string data = dataReader.GetString("data");

               PlayerData playerData = Js.Deserialize<PlayerData>(data);
               dataReader.Close();
               return playerData;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               return null;
          }

     }

     /// <summary>
     /// 保存角色
     /// </summary>
     /// <param name="id"></param>
     /// <param name="playerData"></param>
     /// <returns></returns>
     public static bool UpdatePlayerData(string id,PlayerData playerData)
     {
          string data = Js.Serialize(playerData);
          string sql = $"update player set data='{data} where id='{id}";
          try
          {
               MySqlCommand cmd = new MySqlCommand(sql, mysql);
               cmd.ExecuteNonQuery();
               return true;
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               return false;
          }
     }

     //判定是否字符串安全 防止sql注入
     private static bool isSafeSring(string str)
     {
          return !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\{|\}|%|@|\*|!|\']");
     }
}