using MediatR;

namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// A request that changes something. Kept as its own marker over IRequest so the
/// intent is visible at a glance and so behaviours can be applied to writes only —
/// a transaction or an audit entry has no business wrapping a read.
/// </summary>
public interface ICommand : IRequest;

public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

/// <summary>A request that only reads. Never loads an aggregate to mutate it.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
