using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class ItemService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public ItemModel? NewItem { get; set; }

    public async Task Initialize(ItemsModel? items)
    {
        if (items is not null)
        {
            if (items.Items is null)
            {
                IReadOnlyList<ItemEntity> itms = await _dataAccess.GetItems(items.Id);

                items.Items = itms.Select(i => new ItemModel
                {
                    Id = i.Id,
                    Title = i.Title,
                    IsDone = i.IsDone
                }).ToList();
            }
        }

        NewItem ??= new();
    }

    public async Task AddItem(ItemsModel? items)
    {
        if (items is null || NewItem is null)
            return;

        if (items.Items is null)
            items.Items = new();

        items.Items.Add(NewItem);

        ItemEntity item = new ItemEntity
        {
            ParentId = items.Id,
            Title = NewItem.Title,
            IsDone = false
        };

        await _dataAccess.AddItem(item);

        NewItem.Id = item.Id;

        NewItem = new();
    }
}
