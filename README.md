# UserManagementAPI
User Management API  using ASP.NET Core 

## 1) Code architecture

In root directory, we have the main program Program.cs with the standards steps :

- Create builder

- Add services

- Build Application

- Use Middlewares 

- Endpoints Mapping

- Run application

Models directory to store Data Models :

- User.cs declaring User class :
  - To validate data, I use objects and methods from System.ComponentModel.DataAnnotations;

Middlewares directory to store custom middlewares :

- RequestResponseLoggingMiddleware.cs declaring RequestResponseLoggingMiddleware class

## 2) Packages required to run this API  

To install packqge, use command : dotnet add package <package_name>

List of packages required to run the API :
* Swashbuckle.AspNetCore
* Microsoft.AspNetCore.Authentication.JwtBearer
* System.IdentityModel.Tokens.Jwt

## 3) Test the API with Swagger

Launch the application in terminal by typing command : dotnet run

In a browser, open the URL : http://localhost:5000/swagger/

![](img/swagger_img_01.png)

Login via the /login endpoint with username = admin then click on Execute

![](img/swagger_img_02.png)

In the Response Body, copy the token's value 

Then clik on Authorize Button in the top right corner.

![](img/swagger_img_03.png)

Type in value text box : Bearer followed by one space bar and then paste the token's value copied bellow

![](img/swagger_img_04.png)

Then click on Authorize then on Close.

![](img/swagger_img_05.png)

Your are now ready to test the API's endpoints.

Note that whenever you run the logout endpoints the token is unvalidated and you have to login to get a new authentication

