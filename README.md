# microsoft-dynamics-365-service

Disclaimer: This component was built by the community at large and is not an official Coveo JSUI Component. Use this component at your own risk.

## Getting Started

1. Install the component into your project.

```
npm i @coveops/microsoft-dynamics-365-service
```

2. Use the Component or extend it

Typescript:

```javascript
import { microsoft-dynamics-365-service, Imicrosoft-dynamics-365-serviceOptions } from '@coveops/microsoft-dynamics-365-service';
```

Javascript

```javascript
const microsoft-dynamics-365-service = require('@coveops/microsoft-dynamics-365-service').microsoft-dynamics-365-service;
```

3. You can also expose the component alongside other components being built in your project.

```javascript
export * from '@coveops/microsoft-dynamics-365-service'
```

4. Or for quick testing, you can add the script from unpkg

```html
<script src="https://unpkg.com/@coveops/microsoft-dynamics-365-service@latest/dist/index.min.js"></script>
```

> Disclaimer: Unpkg should be used for testing but not for production.

5. Include the component in your template as follows:

Place the component in your markup:

```html
<div class="Coveomicrosoft-dynamics-365-service"></div>
```

## Extending

Extending the component can be done as follows:

```javascript
import { microsoft-dynamics-365-service, Imicrosoft-dynamics-365-serviceOptions } from "@coveops/microsoft-dynamics-365-service";

export interface IExtendedmicrosoft-dynamics-365-serviceOptions extends Imicrosoft-dynamics-365-serviceOptions {}

export class Extendedmicrosoft-dynamics-365-service extends microsoft-dynamics-365-service {}
```

## Contribute

1. Clone the project
2. Copy `.env.dist` to `.env` and update the COVEO_ORG_ID and COVEO_TOKEN fields in the `.env` file to use your Coveo credentials and SERVER_PORT to configure the port of the sandbox - it will use 8080 by default.
3. Build the code base: `npm run build`
4. Serve the sandbox for live development `npm run serve`