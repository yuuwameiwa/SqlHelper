# SqlHelper
SqlHelper is a simple library that makes it easy to work with SQL databases in C#. Uses models and attribute for interaction.

# Getting started
To use SqlHelper, you'll need to create an instance of DataManager with your SQL connection string:
```
DataManager dataManager = new DataManager(connectionString);
```

# Inserting models
Your models should have a TableName attribute and can optionally have an IdentityColumn attribute:
```
[TableName("Users")]
public class UserModel
{
    [IdentityColumn]
    public int? Id { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
}
```

You can use InsertOne to insert a single model:
```
UserModel userNew = new UserModel {Id = null, Login = "Test", Password = "Test"};
    dataManager.InsertOne(userNew);
```

You can use InsertEnumerable to insert multiple models at once. Models can be stored in any IEnumerable(arrays, lists):
```
List<UserModel> userList = new List<UserModel>();
for (int i = 0; i < 1000000; i++)
{
    UserModel testUser = new UserModel { Login = "Test", Password = "Test" };
    userList.Add(testUser);
}
DataManager dataManager = new DataManager("Server=localhost;Database=UserMvc;Integrated Security=True;TrustServerCertificate=True");
dataManager.InsertEnumerable(userList);
```

# Finding Data
You can use FindOne to find the first row that matches your search criteria. Pass anonymous class with TableName property and properties you are looking for:
```
var findUser = new
    {
        TableName = "Users",
        Id = 1,
    };
UserModel foundUser = dataManager.FindOne<UserModel>(findUser);
```
You can use FindAll to find all rows that match your search criteria:

```
var findUser = new
    {
        TableName = "Users",
        Login = Test,
    };
List<UserModel> users = new List<UserModel>();
users = dataManager.FindAll<UserModel>(findUser);
```
