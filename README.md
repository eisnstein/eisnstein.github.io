# My personal Blog.

[danielhoeflehner.tech](https://danielhoeflehner.tech)

## Development

```sh
bundle install
npm ci
npm run dev    # serve locally with livereload
npm run build  # production build into _site/
```

Pushing to `main` builds and deploys the site via GitHub Actions
(`.github/workflows/deploy.yml`).
