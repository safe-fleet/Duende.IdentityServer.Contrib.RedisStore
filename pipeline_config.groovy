libraries{
  core {
    github_packages = true
    code_style = true
    
    archive {
      enabled = true
      configuration = "Release"
      archiveFormat = "nuget"
      publish {
        enabled = true
        source = "github"
      }
      projectFiles = 'Duende.IdentityServer.Contrib.RedisStore/Duende.IdentityServer.Contrib.RedisStore.csproj'
      archiveFolder = "dist"
    }


    semantic_release {
      enabled = true
      name = 'duende-redis-store'
      repository = 'safe-fleet/Duende.IdentityServer.Contrib.RedisStore'
    }
  }

  dotnet {
    build {
      image = "mcr.microsoft.com/dotnet/core/sdk:3.1.412"
      solution = "Duende.IdentityServer.Contrib.RedisStore.sln"
    }

    unit_test {
      args = '/p:CollectCoverage=true /p:CoverletOutputFormat=opencover --filter "FullyQualifiedName~UnitTest"'
      solution = "Duende.IdentityServer.Contrib.RedisStore.sln"
      htmlReport = true
    }
    sonarqube_analysis {
      project_key = "duende-redis-store"
      project_name = "duende-redis-store"
      opencover_reports_path = "Duende.IdentityServer.Contrib.RedisStore.Tests/Duende.IdentityServer.Contrib.RedisStore.Tests/coverage.opencover.xml"
      exclusions = "**/*.spec.ts,**/Audit/**,**/AuthenticationSchemeDb/**,**/ConfigurationDb/**,**/PersistedGrantDb/**,**/Users/**"
      coverage_exclusions = "**/Program.cs,**/Startup.cs"
    }
  }
}