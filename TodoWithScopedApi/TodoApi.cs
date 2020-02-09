﻿using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Todos
{
    public class TodoApi
    {
        private readonly JsonSerializerOptions _options;
        private readonly TodoDbContext _db;

        public TodoApi(TodoDbContext db, JsonSerializerOptions options)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task GetAll(HttpContext context)
        {
            var todos = await _db.Todos.ToListAsync();

            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, todos, _options);
        }

        public async Task Get(HttpContext context)
        {
            var id = (string)context.Request.RouteValues["id"];
            if (id == null || !long.TryParse(id, out var todoId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var todo = await _db.Todos.FindAsync(todoId);
            if (todo == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, todo, _options);
        }

        public async Task Post(HttpContext context)
        {
            var todo = await JsonSerializer.DeserializeAsync<Todo>(context.Request.Body, _options);

            _db.Todos.Add(todo);
            await _db.SaveChangesAsync();
        }

        public async Task Delete(HttpContext context)
        {
            var id = (string)context.Request.RouteValues["id"];
            if (id == null || !long.TryParse(id, out var todoId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var todo = await _db.Todos.FindAsync(todoId);
            if (todo == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            _db.Todos.Remove(todo);
            await _db.SaveChangesAsync();
        }

        public static void MapRoutes(IEndpointRouteBuilder endpoints, ApiActivator<TodoApi> activator)
        {
            endpoints.MapGet("/api/todos", activator(api => api.GetAll));
            endpoints.MapGet("/api/todos/{id}", activator(api => api.Get));
            endpoints.MapPost("/api/todos", activator(api => api.Post));
            endpoints.MapDelete("/api/todos/{id}", activator(api => api.Delete));
        }
    }
}
