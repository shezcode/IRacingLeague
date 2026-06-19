##  Un cliente te pide que desarrolles una aplicacion con una arquitectura por capas en .NET. 
##  La aplicacion ha de ser contenerizada con Docker. 
##  Explica la arquitectura (metodos, clases, responsabilidades, estructura de carpetas) y los comandos necesarios para contenerizarla.

Vamos a crear una aplicacion en .NET con una arquitectura por capas con la idea de separar la app en bloques con responsabilidades bien diferenciadas, de modo que cada capa haga solo una cosa y dependa lo menos posible de las otras capas.
Esto facilita el mantenimiento del codigo, la reutilizacion de codigo y hace que la app sea mas facil de comprender y testear.
Vamos a implementar cada capa como un proyecto independiente (classlib) dentro de una misma solucion global (sln). Las dependencias de los proyectos van siempre hacia dentro, es decir: presentation => business => data => models.
Para conseguir este acoplamiento, las capas no se comunican mediante clases sino mediante sus interfaces, y las dependencias se pasan por el constructor (inyeccion de dependencias).

CAPAS Y RESPONSABILIDADES
* Capa de Modelos (Dominio): Contiene las entidades a modelar. Son clases con atributos y comportamiento propio. No saben nada de BBDD ni de la interfaz.
* Capa de Accesso a Datos (Repositorios): Se encarga de guardar y recuperar los datos. Cada entidad tiene su propia interfaz de repositorio, IUserRepository, con los metodos tipicos: Add, Get, GetAll, Update, Delete, SaveChanges, y una implementacion que cumple con esa interfaz. 
  En el caso de esta aplicacion, la implementacion de los repositorios guarda los datos en ficheros JSON, pero lo importante es que el resto de la app depende de la interfaz unicamente, por tanto, si en un futuro queremos cambiar a una base de datos lo podemos hacer sin tener que cambiar codigo en otras capas de la app.
* Capa de Negocio (Servicios): Contiene la logica de la app y las reglas de negocio. Por cada entidad tendremos un IUserService y UserService, que reciben el repositorio por el constructor y orquestran las operaciones necesarias. Aqui es donde se implementan las reglas que no son un simple guardado de datos, sino que estan los metodos como por ejemplo 'CompleteSale' (completar una venta), que en este ejemplo lo que haria
    seria comprobar que el anuncio del producto esta activo y el producto sigue en venta, comprobar la transaccion, generar un ticket y marcar el anuncio como vendido. Esta capa valida y decide, la de datos solo se encarga de persistir.
* Capa de Presentacion: En el caso de esta aplicacion es una consola, y tiene 2 clases clave: Program, que es el punto de entrada de la app y se encarga de configurar la inyeccion de dependencias, y MenuApp, que muestra el menu principal y los submenus de la aplicacion con los que el usuario final interactua.

ESTRUCTURA
App/
  Models/
    User.cs
  Data/
    IUserRepository.cs
    UserRepository.cs
  Business/
    IUserService.cs
    UserService.cs
  Presentation/
    Program.cs
    MenuApp.cs
  Dockerfile

Contenerizacion en Docker
Vamos a crear un Dockerfile con multi-stage, con una primera fase en la que usaremos el sdk de dotnet para compilar y publicar nuestra app, y una segunda fase mucho mas ligera, en la que solo utilizaremos el runtime en este caso para emplear los minimos recursos posibles para ejecutar la app.
  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  WORKDIR /src
  COPY . .
  RUN dotnet publish Presentation/App.Presentation.csproj -c Release -o
  
  ------------
  FROM mcr.microsoft.com/dotnet/runtime:8.0
  WORKDIR /app
  COPY --from=build /app ./
  ENTRYPOINT ["dotnet", "App.Presentation.dll"]
  
  Los comandos a ejecutar son:
    # construir la imagen
    docker build -t app-cli:1.0 .
    # ejecutar el contenedor de manera interactiva
    docker run -it -p 8009:8009 -v "$(pwd)/data:/app/data" app-cli:1.0
    docker login
    docker tag app-cli:1.0 a28009/app-cli:1.0
    docker push a28009/app-cli:1.0
    docker pull
    docker run -it a28009/app-cli:1.0
