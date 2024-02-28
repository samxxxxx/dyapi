# dyapi 多元API，抖音、快手、B站、头条、西瓜等短视频平台的无水印视频下载解析接口服务，支持小程序一键解析

- ### 日志

 [Serilog](https://serilog.net/)

 - ### AutoMapper

 [AutoMapper](http://automapper.org/)

 - ### http组件

 [flurl](https://flurl.dev/)

# 微信小程序Demo

![](https://github.com/samxxxxx/dyapi/blob/main/img/mini258.jpg)

可保存视频到相册，生成无水印链接

## 怎么用？

git拉取代码
``` git
git clone https://github.com/samxxxxx/dyapi.git
```

- 修改DYApi项目下的`appsettings.Development.json`文件中`ConnectionStrings:Default` Mysql数据库链接服务器地址、用户名、密码，默认链接为：`server=127.0.0.1;port=3306;database=dydb;uid=root;password=.`

- 修改`Serilog`配置，`connectionString`修改实际的用户账户信息。
``` json
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.MySQL" ],
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "MySQL",
        "Args": {
          "connectionString": "server=127.0.0.1;port=3306;database=dydb;uid=root;password=.;CharSet=utf8mb4",
          "tableName": "logging",
          "storeTimestampInUtc": true
        }
      }
    ]
  },
```

数据库迁移

``` bash
cd dyapi/src/DYApi.EntityframeworkCore/

dotnet ef database update
```

完成，运行项目

![](https://github.com/samxxxxx/dyapi/blob/main/img/swagger.png)

# 开发者

<a href="https://github.com/samxxxxx/dyapi/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=samxxxxx/dyapi" />
</a>