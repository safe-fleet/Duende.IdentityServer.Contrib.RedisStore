libraries{
  core {
    node_label = "docker-builds-slave" // Define the jenkins slave to use.
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
      image = "mcr.microsoft.com/dotnet/sdk:6.0"
      solution = "Duende.IdentityServer.Contrib.RedisStore.sln"
    }

    component_test {
      enabled = true
      project = 'Duende.IdentityServer.Contrib.RedisStore.sln /p:CollectCoverage=true /p:CoverletOutputFormat=opencover --filter "FullyQualifiedName~Tests"'
      compose {
        file = "docker-compose.yml"
        hostname_env_name = "CONTAINER_ID"
        short_hostname = false
        time_to_up = 20
        containers_healthcheck = false
      }
    }

    unit_test {
      enabled = false
      solution = ""
    }

    sonarqube_analysis {
      project_key = "duende-redis-store"
      project_name = "duende-redis-store"
      opencover_reports_path = "Duende.IdentityServer.Contrib.RedisStore.Tests/coverage.opencover.xml"
      exclusions = "**/*.spec.ts,**/Audit/**,**/AuthenticationSchemeDb/**,**/ConfigurationDb/**,**/PersistedGrantDb/**,**/Users/**"
      coverage_exclusions = "**/Program.cs,**/Startup.cs"
    }
  }
}