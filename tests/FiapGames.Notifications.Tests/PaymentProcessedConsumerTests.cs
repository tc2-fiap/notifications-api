using FiapGames.Contracts;
using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Consumers;
using FiapGames.Notifications.Api.Domain;
using FiapGames.Notifications.Api.Infrastructure.Email;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FiapGames.Notifications.Tests;

public class PaymentProcessedConsumerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUserProjectionRepository _userProjections = Substitute.For<IUserProjectionRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly PaymentProcessedConsumer _sut;

    public PaymentProcessedConsumerTests()
    {
        _emailSender.Channel.Returns(DeliveryChannel.Console);
        var logger = Substitute.For<ILogger<PaymentProcessedConsumer>>();
        _sut = new PaymentProcessedConsumer(_repository, _userProjections, _emailSender, logger);
    }

    [Fact]
    public async Task Consume_WhenNoUserProjectionExists_LogsAndSkipsWithoutThrowing()
    {
        var message = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), PaymentStatus.Approved);
        _userProjections.GetAsync(message.UserId, Arg.Any<CancellationToken>()).Returns((UserProjection?)null);

        var context = Substitute.For<ConsumeContext<PaymentProcessedEvent>>();
        context.Message.Returns(message);

        var exception = await Record.ExceptionAsync(() => _sut.Consume(context));

        Assert.Null(exception);
        await _repository.DidNotReceiveWithAnyArgs().TryClaimAsync(default!, default);
    }

    [Fact]
    public async Task Consume_IsKeyedOnOrderId_RegardlessOfStatus()
    {
        var orderId = Guid.NewGuid();
        var message = new PaymentProcessedEvent(orderId, Guid.NewGuid(), PaymentStatus.Approved);
        _userProjections.GetAsync(message.UserId, Arg.Any<CancellationToken>())
            .Returns(new UserProjection(message.UserId, "Jane Doe", "jane@example.com"));
        _repository.TryClaimAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>()).Returns(true);
        _emailSender.SendAsync("jane@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(true, "{}", "{}"));

        var context = Substitute.For<ConsumeContext<PaymentProcessedEvent>>();
        context.Message.Returns(message);

        await _sut.Consume(context);

        await _repository.Received(1).TryClaimAsync(
            Arg.Is<Notification>(n => n.DedupeKey == $"payment-processed:{orderId}"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenAlreadySent_SkipsWithoutSendingEmailAgain()
    {
        var message = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), PaymentStatus.Rejected);
        _userProjections.GetAsync(message.UserId, Arg.Any<CancellationToken>())
            .Returns(new UserProjection(message.UserId, "Jane Doe", "jane@example.com"));
        _repository.TryClaimAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>()).Returns(false);

        var context = Substitute.For<ConsumeContext<PaymentProcessedEvent>>();
        context.Message.Returns(message);

        var exception = await Record.ExceptionAsync(() => _sut.Consume(context));

        Assert.Null(exception);
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }
}
