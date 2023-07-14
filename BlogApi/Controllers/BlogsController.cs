using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BlogApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private string connectionString = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=blogs-dev;Trusted_Connection=True;";
        private readonly AppContext appContext;

        public BlogsController(AppContext appContext)
        {
            this.appContext = appContext;
        }

        // GET: api/<BlogsController>
        [HttpGet]
        public ActionResult<IEnumerable<object>> Get()
        {
            var queryString = @"select * from Blogs";
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(queryString, connection);
            connection.Open();
            var reader = command.ExecuteReader();
            var result = new List<object>();
            while (reader.Read())
            {
                result.Add(new { id = reader["Id"], userId = reader["UserId"], text = reader["Text"], views = reader["Views"] });
            }

            return Ok(result);
        }

        // GET api/<BlogsController>/5
        [HttpGet("{id}")]
        public ActionResult<object> Get(int id)
        {
            var queryString = $"select * from Blogs where Id = {id}";
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(queryString, connection);
            connection.Open();
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                return Ok(new { id = reader["Id"], userId = reader["UserId"], text = reader["Text"] });
            }

            return NotFound();
        }

        // POST api/<BlogsController>
        [HttpPost]
        public ActionResult Post([FromBody] Blog value)
        {
            var userResult = CheckUser(HttpContext) switch
            {
                (true, null) => "",
                (bool, string) r when r.message.Contains("Login does not exist") => throw new Exception(r.message),
                _ => throw new NotImplementedException(),
            };

            var queryString = $"insert into Blogs (Id, UserId, Text) values ({value.Id}, {value.UserId}, '{value.Text}')";
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(queryString, connection);
            connection.Open();
            command.ExecuteNonQuery();
            return Ok();
        }

        // PUT api/<BlogsController>/5
        [HttpPut("{id}")]
        public ActionResult Put(int id, [FromBody] Blog value)
        {
            CheckUser(HttpContext);
            var queryString = $"update Blogs set Id = {value.Id}, UserId = {value.UserId}, Text = '{value.Text}' where Id = {id}";
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(queryString, connection);
            connection.Open();

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex) when (ex.Message.Contains("Id does not exist")) 
            {
                return NotFound();
            }

            return Ok();
        }

        // DELETE api/<BlogsController>/5
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            CheckUser(HttpContext);
            var queryString = $"delete from [Blogs] where Id = {id}";
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(queryString, connection);
            connection.Open();
            command.ExecuteNonQuery();
            return Ok();
        }

        private (bool result, string? message) CheckUser(HttpContext httpContext)
        {
            var login = httpContext.Request.Headers["login"].First();
            var password = httpContext.Request.Headers["password"].First();

            var queryString = $"select * from Users where Login = '{login}'";
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(queryString, connection);
            connection.Open();
            var reader = command.ExecuteReaderAsync().Result;
            while (reader.Read())
            {
                if ((string)reader["Login"] is null or "")
                {
                    return (false, "Login does not exist");
                }

                if ((string)reader["Password"] != password)
                {
                    return (false, "Incorrect password");
                }

                return (true, null);
            }

            return (false, "Unknown state");
        }
    }

    public record Blog(int Id, int UserId, string Text);
}
