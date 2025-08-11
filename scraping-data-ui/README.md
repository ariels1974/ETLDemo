# Scraping Data UI

This is a simple React app that fetches and displays the `ProductScrapingRecord` collection from the ScrapingDataApi.

## Getting Started

1. Make sure the ScrapingDataApi is running and accessible.
2. Run the app:
   ```sh
   npm run dev
   ```
3. The app will be available at http://localhost:5173 by default.

## Configuration

- The API endpoint is set to `/api/products` by default. Update the fetch URL in `src/App.jsx` if your API runs on a different host or port.

## Features

- Fetches and displays product scraping records in a table.

---

This project was bootstrapped with [Vite](https://vitejs.dev/) and [React](https://react.dev/).

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.
