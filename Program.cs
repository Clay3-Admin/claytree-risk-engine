using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Options;
using Claytree.Risk.Functions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
 

var host = new HostBuilder()
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        // Options
        services.AddOptions<SqlOptions>()
            .Bind(ctx.Configuration.GetSection("SQL"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BlobOptions>()
            .Bind(ctx.Configuration.GetSection("BLOB"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<UploadOptions>()
            .Bind(ctx.Configuration.GetSection(UploadOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CosmosOptions>()
            .Bind(ctx.Configuration.GetSection(CosmosOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ✅ NEW: AppNo config (Azure: AppNo__X, Local: local.settings.json)
        services.AddOptions<ApplicationNumberOptions>()
            .Bind(ctx.Configuration.GetSection("AppNo"))
            .ValidateOnStart();
        // NEW: File validation config
        services.AddOptions<FileValidationOptions>()
            .Bind(ctx.Configuration.GetSection("FileValidation"))
            .ValidateOnStart();

        services.AddOptions<DocumentIntelligenceOptions>()
    .Bind(ctx.Configuration.GetSection(DocumentIntelligenceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

        // Services
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<ICosmosReferralRepository, CosmosReferralRepository>();

        // ✅ NEW: Repository + Service for non-inline SQL approach
        services.AddSingleton<ILoanRepository, LoanRepository>();
        services.AddSingleton<ILoanDocumentRepository, LoanDocumentRepository>();
        services.AddSingleton<IApplicationNumberService, ApplicationNumberService>();
        services.AddSingleton<IUploadService, UploadService>();
        services.AddSingleton<IValidationService, ValidationService>();

        // NEW: Structural validation service
        services.AddSingleton<IFileStructuralValidator, FileStructuralValidator>();

        // NEW: Security validation service
        services.AddSingleton<ISecurityFileValidator, SecurityFileValidator>();

        //New: DUPLICATE detection services
        services.AddSingleton<IFileFingerprintService, FileFingerprintService>();
        services.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddSingleton<ILoanDocumentFingerprintRepository, LoanDocumentFingerprintRepository>();

        services.AddSingleton<ILoanDocumentProcessingRepository, LoanDocumentProcessingRepository>();
        //services.AddSingleton<IDocumentExtractor, MockDocumentExtractor>();
        services.AddSingleton<IDocumentExtractor, AzureDocumentIntelligenceExtractor>();

        // HttpClient
        services.AddHttpClient();
    })
    .Build();

host.Run();
