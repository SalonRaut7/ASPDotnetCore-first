using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Web;

var builder = WebApplication.CreateBuilder(args); //builds kestrel server and host
var app = builder.Build(); // web app object

// app.MapGet("/", () => "Hello World!"); //middleware pipeline component

app.Run(async (HttpContext context) =>  // this basically is a middleware component that handles all requests, 
// here we are using the HttpContext object to access request and response details, httpcontext is created for each request
// and passed through the middleware pipeline, we use async to avoid blocking the thread while waiting for I/O operations, await is used to asynchronously wait for the completion of those operations
//the context.Response is awaited to ensure that the response is fully written before the method completes.
{
    // Set response type to text/plain for readability
    context.Response.ContentType = "text/plain";

    // Handle different HTTP methods
    if (context.Request.Method == "GET")
    {
        if (context.Request.Path.StartsWithSegments("/"))
        {
            await context.Response.WriteAsync($"The method used is: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"The URL is: {context.Request.Path}\r\n");

            await context.Response.WriteAsync("Headers:\r\n");
            foreach (var header in context.Request.Headers)
            {
                await context.Response.WriteAsync($"{header.Key}: {header.Value}\r\n");
            }
        }
        else if (context.Request.Path.StartsWithSegments("/employees"))
        {
            var employees = EmployeesRepository.GetAllEmployees();
            foreach (var emp in employees)
            {
                await context.Response.WriteAsync($"ID: {emp.Id}, Name: {emp.Name}, Position: {emp.Position}, Salary: {emp.Salary}\r\n");
            }
        }
    }

    else if (context.Request.Method == "POST")
    {
        // POST is used to create new employee
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Expecting format: name=John&position=Developer&salary=70000
            var queryParams = HttpUtility.ParseQueryString(body);

            // Fix null warnings with ?? ""
            string name = queryParams["name"] ?? "";
            string position = queryParams["position"] ?? "";
            double salary = double.TryParse(queryParams["salary"], out var s) ? s : 0;

            var newEmp = EmployeesRepository.AddEmployee(name, position, salary);
            await context.Response.WriteAsync($"Employee created with ID: {newEmp.Id}\r\n");
        }
    }

    else if (context.Request.Method == "PUT")
    {
        // PUT is used to update existing employee
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var queryParams = HttpUtility.ParseQueryString(body);

            int id = int.TryParse(queryParams["id"], out var empId) ? empId : 0;
            string name = queryParams["name"] ?? "";
            string position = queryParams["position"] ?? "";
            double salary = double.TryParse(queryParams["salary"], out var s) ? s : 0;

            bool updated = EmployeesRepository.UpdateEmployee(id, name, position, salary);

            if (updated)
                await context.Response.WriteAsync($"Employee with ID {id} updated successfully.\r\n");
            else
                await context.Response.WriteAsync($"Employee with ID {id} not found.\r\n");
        }
    }

    else if (context.Request.Method == "DELETE")
    {
        // DELETE is used to remove employee by ID
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            var query = context.Request.Query;
            int id = int.TryParse(query["id"], out var empId) ? empId : 0;

            bool deleted = EmployeesRepository.DeleteEmployee(id);

            if (deleted)
                await context.Response.WriteAsync($"Employee with ID {id} deleted successfully.\r\n");
            else
                await context.Response.WriteAsync($"Employee with ID {id} not found.\r\n");
        }
    }

    else
    {
        context.Response.StatusCode = 405; // Method Not Allowed
        await context.Response.WriteAsync("Method not supported.\r\n");
    }

});

app.Run(); //starts listening for requests, converts request to httpcontext and passes it to the middleware pipeline


// ==================== Repository and Model ====================

static class EmployeesRepository //static class is the class that cannot be instantiated and can only contain static members
//in this case we are using a static class to hold the employee data and a static method to retrieve the data
{
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Software Engineer", 60000),
        new Employee(2, "Jane Smith", "Project Manager", 75000),
        new Employee(3, "Sam Brown", "QA Analyst", 50000)
    };

    private static int nextId = 4;

    public static List<Employee> GetAllEmployees() => employees;

    public static Employee AddEmployee(string name, string position, double salary)
    {
        var emp = new Employee(nextId++, name, position, salary);
        employees.Add(emp);
        return emp;
    }

    public static bool UpdateEmployee(int id, string name, string position, double salary)
    {
        var emp = employees.FirstOrDefault(e => e.Id == id);
        if (emp == null) return false;

        if (!string.IsNullOrEmpty(name)) emp.Name = name;
        if (!string.IsNullOrEmpty(position)) emp.Position = position;
        if (salary > 0) emp.Salary = salary;
        return true;
    }

    public static bool DeleteEmployee(int id)
    {
        var emp = employees.FirstOrDefault(e => e.Id == id);
        if (emp == null) return false;
        employees.Remove(emp);
        return true;
    }
}

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
