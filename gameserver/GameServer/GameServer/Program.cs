using System;

namespace GameServer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (!DbManager.Connect("account","127.0.0.1",3306,"root",""))
            {
                Console.WriteLine("链接数据库失败!!!");
                return;
            }
            //测试
            if (DbManager.Register("lsp","123456"))
            {
                Console.WriteLine("注册成功");
            }

            //创建玩家角色
            if (DbManager.CreatePlayer("zhangdong"))
            {
                Console.WriteLine("角色创建成功");
            }

            Console.WriteLine("链接数据库成功");
            NetManager.StartLoop(8888);
        }
    }
}