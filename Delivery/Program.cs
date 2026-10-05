using System.Globalization;

namespace Delivery;

public class InvalidDeliveryParameterException : Exception
{
    public InvalidDeliveryParameterException() { }

    public InvalidDeliveryParameterException(string message) : base(message) { }

    public InvalidDeliveryParameterException(string message, Exception inner) : base(message, inner) { }
}

public abstract class DeliveryMethod : IComparable<DeliveryMethod>
{
    private string _name = string.Empty;
    private double _ratePerKg;

    public string Name
    {
        get => _name;
        set => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Назва способу доставки не може бути порожньою.")
            : value.Trim();
    }

    public double RatePerKg
    {
        get => _ratePerKg;
        set => _ratePerKg = value > 0
            ? value
            : throw new InvalidDeliveryParameterException("Ставка за кг має бути додатною.");
    }

    public double ReferenceCost => CalculateCost(1);

    protected DeliveryMethod(string name, double ratePerKg)
    {
        Name = name;
        RatePerKg = ratePerKg;
    }

    public abstract string TypeName { get; }

    public abstract double CalculateCost(double weightKg);

    protected static void ValidateWeight(double weightKg)
    {
        if (weightKg < 0)
        {
            throw new InvalidDeliveryParameterException("Вага посилки не може бути від'ємною.");
        }
    }

    public virtual string GetInfo() =>
        $"[{TypeName}] {Name}: ставка {RatePerKg:F2} грн/кг";

    public int CompareTo(DeliveryMethod? other) =>
        other is null ? 1 : ReferenceCost.CompareTo(other.ReferenceCost);
}

public class StandardDelivery : DeliveryMethod
{
    public StandardDelivery(string name, double ratePerKg) : base(name, ratePerKg)
    {
    }

    public StandardDelivery(double ratePerKg) : this("Стандартна доставка", ratePerKg)
    {
    }

    public override string TypeName => "Стандартна";

    public override double CalculateCost(double weightKg)
    {
        ValidateWeight(weightKg);
        return weightKg * RatePerKg;
    }
}

public class ExpressDelivery : DeliveryMethod
{
    private double _speedMultiplier;
    private double _surcharge;

    public double SpeedMultiplier
    {
        get => _speedMultiplier;
        set => _speedMultiplier = value >= 1
            ? value
            : throw new InvalidDeliveryParameterException("Коефіцієнт швидкості має бути не меншим за 1.");
    }

    public double Surcharge
    {
        get => _surcharge;
        set => _surcharge = value >= 0
            ? value
            : throw new InvalidDeliveryParameterException("Надбавка не може бути від'ємною.");
    }

    public ExpressDelivery(string name, double ratePerKg, double speedMultiplier, double surcharge)
        : base(name, ratePerKg)
    {
        SpeedMultiplier = speedMultiplier;
        Surcharge = surcharge;
    }

    public ExpressDelivery(string name, double ratePerKg) : this(name, ratePerKg, 1.5, 50)
    {
    }

    public override string TypeName => "Експрес";

    public override double CalculateCost(double weightKg)
    {
        ValidateWeight(weightKg);
        return weightKg * RatePerKg * SpeedMultiplier + Surcharge;
    }

    public override string GetInfo() =>
        $"{base.GetInfo()}, коефіцієнт швидкості x{SpeedMultiplier:F2}, надбавка {Surcharge:F2} грн";
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        List<DeliveryMethod> methods = new()
        {
            new StandardDelivery("Укрпошта", 20),
            new StandardDelivery(25),
            new ExpressDelivery("Нова пошта Експрес", 30, 1.5, 40),
            new ExpressDelivery("Meest Експрес", 28)
        };

        bool isRunning = true;

        while (isRunning)
        {
            Console.WriteLine();
            Console.WriteLine("=== Довідник способів доставки ===");
            Console.WriteLine("0. Вихід");
            Console.WriteLine("1. Додати спосіб доставки");
            Console.WriteLine("2. Розрахувати вартість доставки");
            Console.WriteLine("3. Показати всі способи доставки");
            Console.WriteLine("4. Відсортувати від найдешевшого до найдорожчого");

            int choice = ReadInt("Ваш вибір: ", 0, 4);

            isRunning = choice switch
            {
                1 => HandleAdd(methods),
                2 => HandleCalculate(methods),
                3 => HandleShowAll(methods),
                4 => HandleSort(methods),
                _ => false
            };
        }

        Console.WriteLine("Роботу завершено.");
    }

    private static bool HandleAdd(List<DeliveryMethod> methods)
    {
        Console.WriteLine("Тип доставки: 1 - стандартна, 2 - експрес");
        int type = ReadInt("Ваш вибір: ", 1, 2);

        try
        {
            Console.Write("Назва: ");
            string name = Console.ReadLine() ?? string.Empty;
            double rate = ReadDouble("Ставка за кг (грн): ");

            DeliveryMethod method;

            if (type == 1)
            {
                method = new StandardDelivery(name, rate);
            }
            else
            {
                double multiplier = ReadDouble("Коефіцієнт швидкості (>= 1): ");
                double surcharge = ReadDouble("Надбавка (грн): ");
                method = new ExpressDelivery(name, rate, multiplier, surcharge);
            }

            methods.Add(method);
            Console.WriteLine($"Додано: {method.GetInfo()}");
        }
        catch (InvalidDeliveryParameterException ex)
        {
            Console.WriteLine($"Помилка параметрів: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Помилка введення: {ex.Message}");
        }

        return true;
    }

    private static bool HandleCalculate(List<DeliveryMethod> methods)
    {
        if (methods.Count == 0)
        {
            Console.WriteLine("Довідник порожній.");
            return true;
        }

        PrintList(methods);
        int index = ReadInt("Оберіть номер способу доставки: ", 1, methods.Count) - 1;
        DeliveryMethod method = methods[index];

        try
        {
            double weight = ReadDouble("Вага посилки (кг): ");
            double cost = method.CalculateCost(weight);
            Console.WriteLine($"Вартість доставки \"{method.Name}\" для {weight:F2} кг: {cost:F2} грн");
        }
        catch (InvalidDeliveryParameterException ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Непередбачена помилка: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Розрахунок завершено.");
        }

        return true;
    }

    private static bool HandleShowAll(List<DeliveryMethod> methods)
    {
        if (methods.Count == 0)
        {
            Console.WriteLine("Довідник порожній.");
            return true;
        }

        PrintList(methods);
        return true;
    }

    private static bool HandleSort(List<DeliveryMethod> methods)
    {
        if (methods.Count == 0)
        {
            Console.WriteLine("Довідник порожній.");
            return true;
        }

        methods.Sort();
        Console.WriteLine("Способи доставки від найдешевшого до найдорожчого (вартість за 1 кг):");

        int number = 1;
        foreach (DeliveryMethod method in methods)
        {
            Console.WriteLine($"{number,2}. {method.ReferenceCost,10:F2} грн  {method.GetInfo()}");
            number++;
        }

        return true;
    }

    private static void PrintList(List<DeliveryMethod> methods)
    {
        int number = 1;
        foreach (DeliveryMethod method in methods)
        {
            Console.WriteLine($"{number,2}. {method.GetInfo()}");
            number++;
        }
    }

    private static double ReadDouble(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (Console.ReadLine() ?? string.Empty).Trim('\uFEFF', ' ').Replace(',', '.');

            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                && double.IsFinite(value))
            {
                return value;
            }

            Console.WriteLine("Некоректне значення. Введіть число.");
        }
    }

    private static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine()?.Trim('\uFEFF', ' ');

            if (int.TryParse(input, out int value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine($"Некоректне значення. Введіть ціле число від {min} до {max}.");
        }
    }
}
