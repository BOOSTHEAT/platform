# Designer

## How To Create the Docker Build Configuration File

In the `ImpliciX.Designer.App` project folder, a configuration file named `build_config.txt` is required. This file is crucial for the build process, so please ensure to add and configure it correctly.

Follow the steps below:

1. **Adding the file**: Create a file named `build_config.txt` in the root of the `ImpliciX.Designer.App` project folder.

2. **Setting file properties**: After adding the file, you need to set its properties in the solution explorer. Right-click on the `build_config.txt` file, select Properties, and set the "Copy to output directory" property to "Copy if newer". This ensures that the latest version of the configuration file is always used during the build process.

3. **File content**: The `build_config.txt` file should contain the following key-value pairs, which are used to authenticate to the NuGet feed:

    ```
    nuget_feed_username=firstname.lastname@boostheat.com
    nuget_feed_password=your_personal_access_token
    ```

Please replace the username and password with your actual NuGet feed credentials. Be careful not to share these credentials as they provide access to your NuGet packages.

> **Personal Access Token**: To create a Personal Access Token (PAT) in Azure DevOps: sign in, select "Personal access tokens" from your profile, click "New Token", name it, set an expiration date, select "Custom defined" under "Scopes", check "Packaging" > "Read", and click "Create".

---

