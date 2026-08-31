using FiapGames.Contracts;
using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Consumers;
using FiapGames.Notifications.Api.Domain;
using FiapGames.Notifications.Api.Infrastructure.Email;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FiapGames.Notifications.Tests;

public class UserCreatedConsumerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUserProjectionRepository _userProjections = Substitute.For<IUserProjectionRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly UserCreatedConsumer _sut;

    public UserCreatedConsumerTests()
    {
        _emailSender.Channel.Returns(DeliveryChannel.Console);
        var logger = Substitute.For<ILogger<UserCreatedConsumer>>();
        _sut = new UserCreatedConsumer(_repository, _userProjections, _emailSender, logger);
    }

    [Fact]
    public async Task Consume_FirstDelivery_UpsertsProjectionAndSendsEmail()
    {
        var userId = Guid.NewGuid();
        var message = new UserCreatedEvent(userId, "Jane Doe", "jane@example.com");
        _repository.TryClaimAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>()).Returns(true);
        _emailSender.SendAsync("jane@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(true, "{}", "{}"));

        var context = Substitute.For<ConsumeContext<UserCreatedEvent>>();
        context.Message.Returns(message);

        await _sut.Consume(context);

        await _userProjections.Received(1).UpsertAsync(userId, "Jane Doe", "jane@example.com", Arg.Any<CancellationToken>());
        await _repository.Received(1).TryClaimAsync(
            Arg.Is<Notification>(n => n.DedupeKey == $"user-created:{userId}"), Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync("jane@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_RedeliveredEvent_StillUpdatesProjectionButDoesNotSendAgain()
    {
        var userId = Guid.NewGuid();
        var message = new UserCreatedEvent(userId, "Jane Doe", "jane@example.com");
        _repository.TryClaimAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>()).Returns(false);

        var context = Substitute.For<ConsumeContext<UserCreatedEvent>>();
        context.Message.Returns(message);

        await _sut.Consume(context);

        await _userProjections.Received(1).UpsertAsync(userId, "Jane Doe", "jane@example.com", Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }
}
