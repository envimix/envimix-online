using EnvimixWebAPI.Dtos;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EnvimixWebAPI.Endpoints;

public static class UserEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("User");

        group.MapPost("{login}", PostUser).RequireAuthorization(Policies.SuperAdminPolicy);
        group.MapGet("{login}", GetUser).RequireAuthorization(Policies.SuperAdminPolicy);
    }

    private static async Task<Results<Ok<UserDto>, BadRequest<ValidationFailureResponse>>> PostUser(
        string login,
        [FromBody] UpdateUserRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        if (!Validator.ValidateLogin(login))
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Invalid login"));
        }

        if (!Validator.ValidateNickname(request.Nickname))
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Invalid nickname"));
        }

        var result = await userService.UpdateUserAsync(login, request, cancellationToken);

        return result.Match<Results<Ok<UserDto>, BadRequest<ValidationFailureResponse>>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure)
        );
    }

    private static async Task<Results<Ok<UserDto>, NotFound, BadRequest<string>>> GetUser(
        string login,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        if (!Validator.ValidateLogin(login))
        {
            return TypedResults.BadRequest("Invalid login");
        }

        var dto = await userService.GetUserDtoByLoginAsync(login, cancellationToken);

        return dto is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(dto);
    }
}
