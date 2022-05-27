This is a [Next.js](https://nextjs.org/) project bootstrapped with [`create-next-app`](https://github.com/vercel/next.js/tree/canary/packages/create-next-app).

# Using with NextjsStaticHosting.AspNetCore

## 1: Install dependencies

  ```bash
  npm install
  ```

## 2: Run or deploy

### 2.1: Local development

1. Start the Next.js dev server:

  ```bash
  npm run dev
  ```

2. Launch the ASP .NET Core project in development mode

  Run ActualNextjsApp.Server in Visual Studio

### 2.2: Deploy to production

1. Build and export the Next.js app to generate static files:

  ```bash
  npm run build-export
  ```

2. Publish the ASP .NET Core project

  When you publish ActualNextjsApp.Server, it will include the outputs generated at `./out`
