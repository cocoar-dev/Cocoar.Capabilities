import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'
import llmstxt from 'vitepress-plugin-llms'

export default withMermaid(
  defineConfig({
    ignoreDeadLinks: false,

    title: 'Cocoar.Capabilities',
    description: 'High-performance capability composition for .NET',

    head: [
      ['link', { rel: 'icon', type: 'image/svg+xml', href: '/logo_light.svg' }],
      ['link', { rel: 'alternate', type: 'text/plain', href: '/llms.txt', title: 'LLM documentation (summary)' }],
      ['link', { rel: 'alternate', type: 'text/plain', href: '/llms-full.txt', title: 'LLM documentation (full)' }],
    ],

    vite: {
      plugins: [llmstxt({
        excludeUnnecessaryFiles: false,
        ignoreFiles: ['changelog.md'],
      })],
    },

    themeConfig: {
      logo: {
        light: '/logo_light.svg',
        dark: '/logo_dark.svg',
      },

      siteTitle: 'Cocoar.Capabilities v1.2',

      nav: [
        { text: 'Guide', link: '/guide/getting-started' },
        { text: 'Reference', link: '/reference/api' },
        { text: 'Changelog', link: '/changelog' },
        { text: 'LLM Docs', link: '/llms-full.txt', target: '_blank' },
        { text: 'NuGet', link: 'https://www.nuget.org/packages/Cocoar.Capabilities' },
      ],

      sidebar: {
        '/guide/': [
          {
            text: 'Introduction',
            items: [
              { text: 'Getting Started', link: '/guide/getting-started' },
              { text: 'Why Capabilities?', link: '/guide/why-capabilities' },
            ],
          },
          {
            text: 'Core Concepts',
            items: [
              { text: 'CapabilityScope', link: '/guide/core/capability-scope' },
              { text: 'Composer', link: '/guide/core/composer' },
              { text: 'Composition', link: '/guide/core/composition' },
              { text: 'Primary Capabilities', link: '/guide/core/primary-capabilities' },
            ],
          },
          {
            text: 'Composition Techniques',
            items: [
              { text: 'Ordering', link: '/guide/composition/ordering' },
              { text: 'Multiple Contracts', link: '/guide/composition/multiple-contracts' },
              { text: 'Recomposition', link: '/guide/composition/recomposition' },
              { text: 'Using* Extensions', link: '/guide/composition/using-extensions' },
            ],
          },
          {
            text: 'Patterns & Inspiration',
            items: [
              { text: 'Patterns & Inspiration', link: '/guide/patterns' },
            ],
          },
          {
            text: 'Scope Context',
            items: [
              { text: 'Owner API', link: '/guide/scope-context/owner-api' },
              { text: 'Anchors API', link: '/guide/scope-context/anchors-api' },
              { text: 'Strongly-Typed Scopes', link: '/guide/scope-context/typed-scopes' },
            ],
          },
          {
            text: 'Advanced',
            items: [
              { text: 'Registries <span class="badge-adv" title="Advanced topic"></span>', link: '/guide/advanced/registries' },
              { text: 'Custom Key Mapping <span class="badge-adv" title="Advanced topic"></span>', link: '/guide/advanced/custom-key-mapping' },
            ],
          },
        ],
        '/reference/': [
          {
            text: 'Reference',
            items: [
              { text: 'API Overview', link: '/reference/api' },
              { text: 'Configuration Options', link: '/reference/options' },
              { text: 'Examples', link: '/reference/examples' },
            ],
          },
        ],
      },

      socialLinks: [
        { icon: 'github', link: 'https://github.com/cocoar-dev/Cocoar.Capabilities' },
      ],

      search: {
        provider: 'local',
      },

      footer: {
        message: 'Released under the Apache-2.0 License.',
        copyright: 'Copyright 2025-present Cocoar',
      },
    },

    mermaid: {},

    mermaidPlugin: {
      class: 'mermaid',
    },
  }),
)
