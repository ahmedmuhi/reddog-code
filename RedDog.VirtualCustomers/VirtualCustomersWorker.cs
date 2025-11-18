using System.Net.Http;
using Dapr.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedDog.Shared;
using RedDog.VirtualCustomers.Configuration;
using RedDog.VirtualCustomers.Models;

namespace RedDog.VirtualCustomers;

public sealed class VirtualCustomersWorker : BackgroundService
{
    private const string OrderServiceDaprId = "order-service";

    private readonly IHostApplicationLifetime _lifetime;
    private readonly DaprInvocationHelper _invocationHelper;
    private readonly ILogger<VirtualCustomersWorker> _logger;
    private readonly VirtualCustomerOptions _options;
    private readonly Random _random = Random.Shared;

    private List<Product>? _products;

    public VirtualCustomersWorker(
        IHostApplicationLifetime lifetime,
        DaprClient daprClient,
        IOptions<VirtualCustomerOptions> options,
        ILogger<VirtualCustomersWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        
        _lifetime = lifetime;
        _invocationHelper = new DaprInvocationHelper(daprClient);
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Virtual customers starting for store {StoreId}. DisableDaprCalls={Disable} NumOrders={NumOrders}", _options.StoreId, _options.DisableDaprCalls, _options.NumOrders);

        if (_options.DisableDaprCalls)
        {
            _products = new List<Product>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Smoke Latte",
                    Description = "Placeholder product for smoke tests",
                    UnitCost = 2.5m,
                    UnitPrice = 4.5m,
                    ImageUrl = string.Empty
                },
                new()
                {
                    ProductId = 2,
                    ProductName = "Test Espresso",
                    Description = "Placeholder product",
                    UnitCost = 1.5m,
                    UnitPrice = 3.0m,
                    ImageUrl = string.Empty
                }
            };
        }
        else
        {
            await EnsureProductsLoadedAsync(stoppingToken);
        }

        var ordersCreated = 0;
        while (!stoppingToken.IsCancellationRequested && (_options.NumOrders == -1 || ordersCreated < _options.NumOrders))
        {
            await Task.Delay(RandomDelay(_options.MinSecondsBetweenOrders, _options.MaxSecondsBetweenOrders), stoppingToken);

            if (_options.DisableDaprCalls)
            {
                _logger.LogInformation("[Smoke Test] Simulating order placement");
            }
            else
            {
                await CreateOrderAsync(stoppingToken);
            }

            ordersCreated++;
        }

        _logger.LogInformation("Virtual customers completed execution (Orders Created: {Count})", ordersCreated);
        _lifetime.StopApplication();
    }

    private async Task EnsureProductsLoadedAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _products == null)
        {
            try
            {
                _products = await _invocationHelper.InvokeMethodAsync<List<Product>>(
                    OrderServiceDaprId,
                    "product",
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to retrieve products from OrderService. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task CreateOrderAsync(CancellationToken stoppingToken)
    {
        if (_products == null || _products.Count == 0)
        {
            _logger.LogWarning("No products available; skipping order creation.");
            return;
        }

        var order = BuildRandomOrder();
        _logger.LogInformation("Customer {First} {Last} placing order with {Count} items", order.FirstName, order.LastName, order.OrderItems.Count);

        try
        {
            await _invocationHelper.InvokeMethodAsync(OrderServiceDaprId, "order", order, stoppingToken);
            await Task.Delay(RandomDelay(_options.MinSecondsToPlaceOrder, _options.MaxSecondsToPlaceOrder), stoppingToken);
            _logger.LogInformation("Customer {First} {Last} order submitted", order.FirstName, order.LastName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order for customer {First} {Last}", order.FirstName, order.LastName);
        }
    }

    private CustomerOrder BuildRandomOrder()
    {
        var (id, first, last) = customers[_random.Next(customers.Length)];
        var order = new CustomerOrder
        {
            StoreId = _options.StoreId,
            FirstName = first,
            LastName = last,
            LoyaltyId = id.ToString(),
            OrderItems = new List<CustomerOrderItem>()
        };

        var numProducts = _products!.Count;
        var uniqueItems = Math.Min(numProducts, _options.MaxUniqueItemsPerOrder);
        var itemsToAdd = _random.Next(1, uniqueItems + 1);
        var chosen = new HashSet<int>();

        for (var i = 0; i < itemsToAdd; i++)
        {
            int productIndex;
            do
            {
                productIndex = _products[_random.Next(numProducts)].ProductId;
            }
            while (!chosen.Add(productIndex));

            var quantity = _random.Next(1, _options.MaxItemQuantity + 1);
            order.OrderItems.Add(new CustomerOrderItem
            {
                ProductId = productIndex,
                Quantity = quantity
            });
        }

        return order;
    }

    private TimeSpan RandomDelay(int minSeconds, int maxSeconds)
        => TimeSpan.FromSeconds(_random.Next(minSeconds, maxSeconds + 1));

    // existing list of customers (truncated for brevity)
    private static readonly (int Id, string First, string Last)[] customers =
    {
        (1,"Bruce","Wayne"),(2,"Lou","Redwood"),(3,"Allan","Austin"),(4,"Rusty","Ryan"),(5,"Aubrey","Curtis"),(6,"Aldo","Raine"),(7,"Magrit","Reck"),(8,"Mason","Lucas"),(9,"Etienne","Tremblay"),(10,"Myrtle","Martin"),(11,"Adelgunde","Rode"),(12,"Matt","Wallace"),(13,"Dick","Pepperfield"),(14,"Beatrice","Gagnon"),(15,"Julian","Harris"),(16,"Ellinor","Flick"),(17,"Nils","Rüdiger"),(18,"Bea","Arthur"),(19,"Mickey","O'Neil"),(20,"Cindy","Powell"),(21,"Brielle","Singh"),(22,"Jackie","Moon"),(23,"Ilhan","Kaps"),(24,"Arthur","Grewal"),(25,"Scarlett","Hawkins"),(26,"Brennan","Huff"),(27,"Marilou","Williams"),(28,"Estelle","Getty"),(29,"Camille","Macdonald"),(30,"Danielle","Niemeier"),(31,"Tom","Bishop"),(32,"Dale","Doback"),(33,"Logan","Scott"),(34,"Darlene","Gregory"),(35,"Cal","Naughton"),(36,"Megan","Singh"),(37,"Jeff","Goldblum"),(38,"Cody","Chapman"),(39,"Christina","Snyder"),(40,"Rafi","Cunado"),(41,"Leah","Holmes"),(42,"Brielle","Harcourt"),(43,"Amelia","Edwards"),(44,"Leon","Green"),(45,"Elizabeth","Arnold"),(46,"Ricky","Bobby"),(47,"Hardy","Backhaus"),(48,"Renato","Jahnke"),(49,"Zoe","Bélanger"),(50,"Taco","MacArthur"),(51,"Kelly","Harrison"),(52,"Betty","White"),(53,"Allen","Gamble"),(54,"Alex","Clarke"),(55,"Sandra","Bailey"),(56,"Jacqueline","White"),(57,"Erik","Lehnsherr"),(58,"Joyce","Roberts"),(59,"Lucille","Gutierrez"),(60,"Ashley","Mills"),(61,"Antoine","Thompson"),(62,"Maddison","Hall"),(63,"Franz","Rütten"),(64,"Hildegund","Esch"),(65,"Lorraine","Silva"),(66,"Steffi","Graf"),(67,"Amelia","Hill"),(68,"Sophia","Williams"),(69,"Patricia","Little"),(70,"Peter","Weyland"),(71,"Wanda","Simpson"),(72,"Tyler","Durden"),(73,"Dorothy","Mantooth"),(74,"Sloan","Sabbath"),(75,"Roy","Hobbs"),(76,"Jerry","Rice"),(77,"Christa","Ebert"),(78,"Lily","Grewal"),(79,"Benjamin","Perkins"),(80,"Debra","Smith"),(81,"Elliot","Park"),(82,"Aubree","Williamson"),(83,"Madison","Sanchez"),(84,"Terry","Hoitz"),(85,"Julie","Chavez"),(86,"Russ","Hanneman"),(87,"Daniel","Gagné"),(88,"Bertram","Gilfoyle"),(89,"Emma","Garcia"),(90,"Leo","Spaceman"),(91,"Clarence","Gilbert"),(92,"Jessie","Ray"),(93,"Kenny","Powers"),(94,"Pippa","Wang"),(95,"Jack","Barker"),(96,"Jean-Baptiste","Zorg"),(97,"Joe","Montana"),(98,"Cathy","Perry"),(99,"Valerie","Watkins"),(100,"Abigail","Andersen"),(101,"Megan","Barnaby"),(102,"Erin","Rhodes"),(103,"Lane","Myer"),(104,"Charlie","Lo"),(105,"Monique","Junot"),(106,"Calvin","Joyner"),(107,"Walter","White"),(108,"Mia","Brar"),(109,"MacKenzie","McHale"),(110,"Ty","Webb"),(111,"Allison","Bryant"),(112,"Jesse","Pinkman"),(113,"Natalie","Thompson"),(114,"Ashley","Carlson"),(115,"Gustavo","Fring"),(116,"Danny","Noonan"),(117,"Leah","Thompson"),(118,"Elizabeth","Mitchell"),(119,"John","McClane"),(120,"Archer","Smith"),(121,"Vincent","Hanna"),(122,"Jon","Garrett"),(123,"Dwight","Schrute"),(124,"Vicki","Scott"),(125,"Dwayne","Holt"),(126,"Fletcher","Clarke"),(127,"Grace","Cox"),(128,"Mika","Haubold"),(129,"Joe","Kent"),(130,"Alexandra","Roberts"),(131,"Will","McAvoy"),(132,"Leo","Knight"),(133,"Maeva","Claire"),(134,"Keira","Martin"),(135,"Amanda","Washington"),(136,"Hans","Gruber"),(137,"Judy","Gomez"),(138,"Sergio","Ebeling"),(139,"Quinn","Moore"),(140,"Michael","Bolton"),(141,"Eliza","Grant"),(142,"Jill","Grant"),(143,"Zoltan","Haller"),(144,"Ramon","Ross"),(145,"Carl","Spackler"),(146,"Addison","Park"),(147,"Walter","Sobchak"),(148,"Mia","Mackay"),(149,"Korben","Dallas"),(150,"Milton","Waddams"),(151,"Brent","Rodriguez"),(152,"Melodie","Pelletier"),(153,"Emily","Jackson"),(154,"Neil","McCauley"),(155,"Jessica","Lawrence"),(156,"Julia","Lam"),(157,"Sibilla","Knappe"),(158,"Al","Czervik"),(159,"Harry","Ellis"),(160,"Emma","Williams"),(161,"Kristen","Arnold"),(162,"Heywood","Floyd"),(163,"Hannah","Mackay"),(164,"Larry","Sellers"),(165,"Elihu","Smails"),(166,"Cole","Trickle"),(167,"Rita","Vrataski"),(168,"Peter","Gibbons"),(169,"Kayla","Smith"),(170,"Theo","Morris"),(171,"Edith","Carpenter"),(172,"Sherlock","Holmes"),(173,"David","Bowman"),(174,"Clark","Griswold"),(175,"Marilou","Park"),(176,"Phil","Wenneck"),(177,"Milan","Scholze"),(178,"Irwin","Fletcher"),(179,"Irene","Foster"),(180,"Leeloo","Dallas"),(181,"Jack","Harper"),(182,"Jill","Lambert"),(183,"Linda","Hunter"),(184,"Maritta","Walch"),(185,"Harris","Telemacher"),(186,"Phoebe","Rice"),(187,"Estelle","Rohrer"),(188,"Malcolm","Beech"),(189,"Bill","Lumberg"),(190,"Chris","Kyle"),(191,"Miranda","Priestly"),(192,"Andrea","Sachs"),(193,"Kirk","Lazarus"),(194,"Katniss","Everdeen"),(195,"Vinnie","Barbarino"),(196,"Mia","Hill"),(197,"Les","Grossman"),(198,"Jeffrey","Lebowski"),(199,"Wilma","Ramos"),(200,"Rachel","Alvarez")
    };
}
