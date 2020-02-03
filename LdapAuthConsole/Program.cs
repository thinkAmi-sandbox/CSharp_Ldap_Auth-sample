using System;
using System.Linq;
using Novell.Directory.Ldap;

namespace LdapAuthConsole
{
    class Program
    {
        static string ConvertDomain(string domain)
        {
            var r = domain.Split(".")
                .Select(s => $"dc={s}")
                .Aggregate((result, element) => $"{result}, {element}");
            
            Console.WriteLine(r);
            // => dc=sub, dc=example, dc=co, dc=jp
            
            return r;
        }
        
        static void Main(string[] args)
        {
            var userName = "foo_user";  // Domain Users
            var password = "YOUR_PASSWORD";
            var domain = "sub.example.co.jp";
            var ipAddress = "192.168.xxx.xxx";
            var port = 389;
            var userDn = $"{userName}@{domain}";
            
            try
            {
                using (var connection = new LdapConnection { SecureSocketLayer = false })
                {
                    connection.Connect(ipAddress, port);
                    connection.Bind(userDn, password);

                    if (connection.Bound){
                        Console.WriteLine("接続できました");
                        
                        // 検索
                        var searchBase = ConvertDomain(domain);
                        
                        // あいまい検索が使える
                        var searchFilter = $"(SAMAccountName=*{userName}*)";
                        var result = connection.Search(
                            searchBase,
                            // ドメイン直下からすべてのサブを調べる
                            LdapConnection.ScopeSub,
                            searchFilter,
                            new []
                            {
                                "displayName",        // 表示名
                                "cn",                 // 表示名と同じ
                                "sn",                 // 姓
                                "givenName",          // 名
                                "userPrincipalName",  // ユーザーログオン名
                                "sAMAccountName",     // ユーザーログオン名(Windows 2000以前)
                                "description"         // 説明
                            },
                            false
                        );
                        var user = result.Next();
                        var displayName = user.GetAttribute("displayName").StringValue;
                        
                        Console.WriteLine(user);
                        // =>
                        // LdapEntry: CN=foo bar,CN=Users,DC=sub,DC=example,DC=co,DC=jp;
                        // LdapAttributeSet:
                        //     LdapAttribute: {type='cn', value='foo bar'}
                        //     LdapAttribute: {type='sn', value='foo'}
                        //     LdapAttribute: {type='givenName', value='bar'}
                        //     LdapAttribute: {type='displayName', value='foo bar'}
                        //     LdapAttribute: {type='sAMAccountName', value='foo_user'}
                        //     LdapAttribute: {type='userPrincipalName', value='foo_user@sub.example.co.jp'}
                        
                        Console.WriteLine(displayName);
                        // => foo bar
                    }
                    else
                    {
                        Console.WriteLine("接続できませんでした");
                    }
                }
            }
            catch (LdapException ex)
            {
                // Log exception
                // TODO 例外処理を実装
                Console.WriteLine("例外が出ました");
                Console.WriteLine(ex);
            }
        }
    }
}
