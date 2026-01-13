import { Component, OnInit, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Catalog } from '../../services/catalog';
import { Plate, PagedResult, RevenueStatistics, PlateStatus } from '../../models/plate';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { formatRegistration } from '../../utils/plate-helpers';

// Register Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-plate-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './plate-list.html',
  styleUrl: './plate-list.css'
})
export class PlateListComponent implements OnInit, AfterViewInit {
  plates: Plate[] = [];
  statistics: RevenueStatistics | null = null;
  loading = true;
  error: string | null = null;
  success: string | null = null;
  
  private profitChart: Chart | null = null;

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;
  hasPreviousPage = false;
  hasNextPage = false;

  // Filters
  searchText = '';
  lettersFilter = '';
  numbersFilter: number | undefined = undefined;
  statusFilter: PlateStatus = PlateStatus.ForSale;
  sortBy = '';

  // Sell Modal
  showSellModal = false;
  selectedPlate: Plate | null = null;
  selectedPromoCode = '';
  calculatedPrice = 0;

  // Enum for template
  PlateStatus = PlateStatus;

  constructor(private catalogService: Catalog, private cdr: ChangeDetectorRef) { }

  // Make the utility function accessible to the template
  formatRegistration = formatRegistration;

  ngOnInit(): void {
    this.loadPlates();
    this.loadStatistics();
  }

  ngAfterViewInit(): void {
    // Initialize the chart after view is ready
    this.initializeChart();
  }

  private initializeChart(): void {
    if (!this.statistics || this.statistics.platesSold === 0) {
      return;
    }

    const canvas = document.getElementById('profitMarginChart') as HTMLCanvasElement;
    if (!canvas) {
      return;
    }

    const cost = this.statistics.totalRevenue - this.statistics.totalProfit;

    const config: ChartConfiguration = {
      type: 'doughnut',
      data: {
        labels: ['Profit', 'Cost'],
        datasets: [{
          label: 'Profit Breakdown',
          data: [this.statistics.totalProfit, cost],
          backgroundColor: [
            'rgba(40, 167, 69, 0.8)',   // Green for profit
            'rgba(220, 53, 69, 0.8)'    // Red for cost
          ],
          borderColor: [
            'rgba(40, 167, 69, 1)',
            'rgba(220, 53, 69, 1)'
          ],
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        plugins: {
          legend: {
            position: 'bottom',
          },
          title: {
            display: true,
            text: `Revenue vs Profit - Average Margin: ${(this.statistics.averageProfitMargin * 100).toFixed(2)}%`,
            font: {
              size: 16,
              weight: 'bold'
            }
          },
          tooltip: {
            callbacks: {
              label: (context: any) => {
                const label = context.label || '';
                const value = context.parsed || 0;
                const total = this.statistics!.totalRevenue;
                const percentage = ((value / total) * 100).toFixed(2);
                return `${label}: £${value.toFixed(2)} (${percentage}%)`;
              }
            }
          }
        }
      }
    };

    this.profitChart = new Chart(canvas, config);
  }

  loadPlates(): void {
    this.loading = true;
    this.error = null;

    this.catalogService.getPlates(
      this.searchText || undefined,
      this.lettersFilter || undefined,
      this.numbersFilter,
      this.statusFilter,
      this.sortBy || undefined,
      this.currentPage,
      this.pageSize
    ).pipe(
      catchError(err => {
        console.error('Error loading plates:', err);
        this.error = 'Failed to load plates: ' + (err.message || 'Unknown error');
        this.loading = false;
        return of({ items: [], page: 1, pageSize: 20, totalPages: 0, totalCount: 0, hasPreviousPage: false, hasNextPage: false } as PagedResult<Plate>);
      }),
      finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (data: PagedResult<Plate>) => {
        this.plates = data.items;
        this.currentPage = data.page;
        this.totalPages = data.totalPages;
        this.totalCount = data.totalCount;
        this.hasPreviousPage = data.hasPreviousPage;
        this.hasNextPage = data.hasNextPage;
      },
      error: (err) => {
        this.error = 'Failed to load plates. Please try again later.';
        console.error('Error loading plates:', err);
      }
    });
  }

  loadStatistics(): void {
    this.catalogService.getRevenueStatistics().pipe(
      catchError(err => {
        console.error('Error loading statistics:', err);
        return of(null);
      })
    ).subscribe({
      next: (data) => {
        this.statistics = data;
        
        // Destroy existing chart if it exists
        if (this.profitChart) {
          this.profitChart.destroy();
          this.profitChart = null;
        }
        
        // Reinitialize chart with new data
        setTimeout(() => this.initializeChart(), 100);
      },
      error: (err) => {
        console.error('Error loading statistics:', err);
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadPlates();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadPlates();
  }

  getPages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  reservePlate(plate: Plate): void {
    this.catalogService.reservePlate(plate.id).subscribe({
      next: () => {
        this.success = 'Plate reserved successfully!';
        this.loadPlates();
        this.loadStatistics();
        setTimeout(() => this.success = null, 3000);
      },
      error: (err) => {
        this.error = 'Failed to reserve plate: ' + (err.error?.error || err.message);
        setTimeout(() => this.error = null, 5000);
      }
    });
  }

  unreservePlate(plate: Plate): void {
    this.catalogService.unreservePlate(plate.id).subscribe({
      next: () => {
        this.success = 'Plate unreserved successfully!';
        this.loadPlates();
        setTimeout(() => this.success = null, 3000);
      },
      error: (err) => {
        this.error = 'Failed to unreserve plate: ' + (err.error?.error || err.message);
        setTimeout(() => this.error = null, 5000);
      }
    });
  }

  openSellModal(plate: Plate): void {
    this.selectedPlate = plate;
    this.selectedPromoCode = '';
    this.calculatedPrice = plate.salePrice;
    this.showSellModal = true;
  }

  closeSellModal(): void {
    this.showSellModal = false;
    this.selectedPlate = null;
    this.selectedPromoCode = '';
    this.calculatedPrice = 0;
  }

  onPromoCodeChange(): void {
    if (!this.selectedPlate) return;

    this.catalogService.calculatePrice(
      this.selectedPlate.id,
      this.selectedPromoCode || undefined
    ).subscribe({
      next: (price) => {
        this.calculatedPrice = price;
      },
      error: (err) => {
        console.error('Error calculating price:', err);
      }
    });
  }

  confirmSale(): void {
    if (!this.selectedPlate) return;

    this.catalogService.sellPlate(
      this.selectedPlate.id,
      this.selectedPromoCode || undefined
    ).subscribe({
      next: () => {
        this.success = 'Plate sold successfully!';
        this.closeSellModal();
        this.loadPlates();
        this.loadStatistics();
        setTimeout(() => this.success = null, 3000);
      },
      error: (err) => {
        this.error = 'Failed to sell plate: ' + (err.error?.error || err.message);
        this.closeSellModal();
        setTimeout(() => this.error = null, 5000);
      }
    });
  }

  getStatusBadgeClass(status: PlateStatus): string {
    switch (status) {
      case PlateStatus.ForSale: return 'badge-success';
      case PlateStatus.Reserved: return 'badge-warning';
      case PlateStatus.Sold: return 'badge-secondary';
      default: return 'badge-secondary';
    }
  }

  getStatusText(status: PlateStatus): string {
    switch (status) {
      case PlateStatus.ForSale: return 'For Sale';
      case PlateStatus.Reserved: return 'Reserved';
      case PlateStatus.Sold: return 'Sold';
      default: return 'Unknown';
    }
  }
}
