import { Component, IComponentBindings, ComponentOptions } from 'coveo-search-ui';
import { lazyComponent } from '@coveops/turbo-core';

export interface IMicrosoftDynamics_365ServiceOptions {}

@lazyComponent
export class MicrosoftDynamics_365Service extends Component {
    static ID = 'MicrosoftDynamics_365Service';
    static options: IMicrosoftDynamics_365ServiceOptions = {};

    constructor(public element: HTMLElement, public options: IMicrosoftDynamics_365ServiceOptions, public bindings: IComponentBindings) {
        super(element, MicrosoftDynamics_365Service.ID, bindings);
        this.options = ComponentOptions.initComponentOptions(element, MicrosoftDynamics_365Service, options);
    }
}