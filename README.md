# RouteTableTool
Capture process IPs and write them to the routing table, Run as administrator.
Referenced [CodeCowboyOrg/Ip4RouteTable](https://github.com/CodeCowboyOrg/Ip4RouteTable) project code.

捕捉进程IP并写入路由表,以管理员身份运行。
引用了[CodeCowboyOrg/Ip4RouteTable](https://github.com/CodeCowboyOrg/Ip4RouteTable)项目代码。


# 运行要求
- .NET Framework 4.6

#  功能
- 频率，每2秒捕捉进程远端IP地址。
- 写入，写入当前列表IP至路由表，此功能需要以管理员身份运行程序。
- 撤销，撤销上次写入的路由表，此功能需要以管理员身份运行程序。
- 存储，保存当前列表IP地址至配置文件。
- 导入，从配置文件中读取IP地址并`追加`至当前列表。


