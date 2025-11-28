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

        group.MapPost("", PostUser);
        group.MapGet("{login}", GetUser).RequireAuthorization(Policies.SuperAdminPolicy);
    }

    private static async Task<Results<Ok<AuthenticateUserResponse>, BadRequest<ValidationFailureResponse>>> PostUser(
        [FromBody] AuthenticateUserRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        if (!Validator.ValidateLogin(request.User.Login))
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Invalid login"));
        }

        if (!Validator.ValidateNickname(request.User.Nickname))
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Invalid nickname"));
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Token cannot be empty"));
        }

        var result = await userService.AuthenticateAsync(request, cancellationToken);

        return result.Match<Results<Ok<AuthenticateUserResponse>, BadRequest<ValidationFailureResponse>>>(
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
