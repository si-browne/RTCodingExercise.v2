import { Component, signal } from '@angular/core';
import { PlateListComponent } from './components/plate-list/plate-list';

@Component({
  selector: 'app-root',
  imports: [PlateListComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Regtransfers Plate Shop');
}
