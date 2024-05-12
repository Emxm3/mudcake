namespace MudCake.Data.Infrastructure
{
    public interface IContainer<T, TId>
    {
        TId? Id { get; set; }

        IContainer<T?, TId?>? Parent { get; set; }

        List<IContainer<T?, TId?>?>? Children { get; set; }

        List<T?>? Content { get; set; } 

    }
}
