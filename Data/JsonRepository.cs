using System.Text.Json;

namespace IRacingLeague.Data;

public abstract class JsonRepository<T> where T : class
{
    private readonly string _dataDirectory;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    protected readonly List<T> Items;
    private int _nextId;

    protected JsonRepository(string dataDirectory, string fileName)
    {
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(dataDirectory, fileName);
        Items = Load();
        _nextId = Items.Count == 0 ? 1 : Items.Max(GetId) + 1;
    }

    protected abstract int GetId(T item);
    protected abstract void SetId(T item, int id);

    protected void AddItem(T item)
    {
        SetId(item, _nextId++);
        Items.Add(item);
    }

    public T? Get(int id) => Items.FirstOrDefault(i => GetId(i) == id);

    public IEnumerable<T> GetAll() => Items.ToList();

    public void Update(T item)
    {
        var index = Items.FindIndex(i => GetId(i) == GetId(item));
        if (index >= 0)
            Items[index] = item;
    }

    public void Delete(int id) => Items.RemoveAll(i => GetId(i) == id);

    public void SaveChanges()
    {
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(Items, _options));
    }

    private List<T> Load()
    {
        if (!File.Exists(_filePath))
            return new List<T>();

        string json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }
}
