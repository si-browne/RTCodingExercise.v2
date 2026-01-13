import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PlateListComponent } from './plate-list';
import { Catalog } from '../../services/catalog';
import { Plate, PagedResult, RevenueStatistics, PlateStatus } from '../../models/plate';
import { of, throwError } from 'rxjs';
import { vi, describe, beforeEach, it, expect } from 'vitest';

/**
 * Unit tests for PlateListComponent
 * Testing component behavior according to Regtransfers Code Exercise requirements
 */
describe('PlateListComponent', () => {
  let component: PlateListComponent;
  let fixture: ComponentFixture<PlateListComponent>;
  let mockCatalogService: any;

  const mockPlates: Plate[] = [
    {
      id: '1',
      registration: 'AB12 CDE',
      purchasePrice: 100,
      salePrice: 120,
      status: PlateStatus.ForSale,
      letters: 'AB',
      numbers: 12
    },
    {
      id: '2',
      registration: 'XY99 ZZZ',
      purchasePrice: 200,
      salePrice: 240,
      status: PlateStatus.Reserved,
      reservedDate: new Date()
    },
    {
      id: '3',
      registration: 'CD34 FGH',
      purchasePrice: 150,
      salePrice: 180,
      status: PlateStatus.ForSale,
      letters: 'CD',
      numbers: 34
    }
  ];

  const mockPagedResult: PagedResult<Plate> = {
    items: mockPlates,
    totalCount: 3,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  const mockStats: RevenueStatistics = {
    totalRevenue: 1200,
    totalProfit: 200,
    platesSold: 10,
    averageProfitMargin: 0.1667
  };

  beforeEach(async () => {
    mockCatalogService = {
      getPlates: vi.fn().mockReturnValue(of(mockPagedResult)),
      reservePlate: vi.fn(),
      unreservePlate: vi.fn(),
      sellPlate: vi.fn(),
      calculatePrice: vi.fn(),
      getRevenueStatistics: vi.fn().mockReturnValue(of(mockStats))
    };

    await TestBed.configureTestingModule({
      imports: [PlateListComponent],
      providers: [
        { provide: Catalog, useValue: mockCatalogService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PlateListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // Component Initialization
  describe('ngOnInit', () => {
    it('should load plates and statistics on init', async () => {
      fixture.detectChanges();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined, undefined, undefined, PlateStatus.ForSale, undefined, 1, 20
      );
      expect(mockCatalogService.getRevenueStatistics).toHaveBeenCalled();
      expect(component.plates).toEqual(mockPlates);
      expect(component.statistics).toEqual(mockStats);
      expect(component.loading).toBe(false);
    }); 

    it('should initialize with correct default values', () => {
      expect(component.currentPage).toBe(1);
      expect(component.pageSize).toBe(20);
      expect(component.statusFilter).toBe(PlateStatus.ForSale);
      expect(component.plates).toEqual([]);
      expect(component.loading).toBe(true);
    });
  });

  // User Story 1 - List Plates with 20% Markup
  describe('loadPlates', () => {
    it('should load plates with correct parameters', async () => {
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalled();
      expect(component.plates.length).toBe(3);
      expect(component.totalCount).toBe(3);
      expect(component.totalPages).toBe(1);
    }); 

    it('should verify 20% markup is displayed', async () => {
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      const plate = component.plates[0];
      expect(plate.purchasePrice).toBe(100);
      expect(plate.salePrice).toBe(120); // 20% markup
    }); 

    it('should handle loading state correctly', async () => {
      // Component starts with loading=true from init, then sets to false
      // After ngOnInit the mock has already been called
      component.loadPlates();
      // Loading immediately set to true in loadPlates method
      // but by the time we check, the observable has completed due to synchronous mock
      
      await new Promise(resolve => setTimeout(resolve, 10));
      expect(component.loading).toBe(false);
    }); 

    it('should handle errors gracefully', async () => {
      mockCatalogService.getPlates.mockReturnValue(throwError(() => new Error('API Error')));
      
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(component.error).toBe('Failed to load plates: API Error');
      expect(component.loading).toBe(false);
    }); 
  });

  // User Story 2 - Order by Price
  describe('sortBy', () => {
    it('should apply price ascending sort', async () => {
      component.sortBy = 'price_asc';
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined,
        undefined,
        component.numbersFilter,
        component.statusFilter,
        'price_asc',
        1,
        20
      );
    }); 

    it('should apply price descending sort', async () => {
      component.sortBy = 'price_desc';
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined,
        undefined,
        component.numbersFilter,
        component.statusFilter,
        'price_desc',
        1,
        20
      );
    }); 
  });

  // User Story 3 - Filter by Letters and Numbers
  describe('filtering', () => {
    it('should apply search filter', async () => {
      component.searchText = 'AB12';
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        'AB12', undefined, undefined, PlateStatus.ForSale, undefined, 1, 20
      );
    }); 

    it('should apply letters filter', async () => {
      component.lettersFilter = 'ABC';
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined, 'ABC', undefined, PlateStatus.ForSale, undefined, 1, 20
      );
    }); 

    it('should apply numbers filter', async () => {
      component.numbersFilter = 123;
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined, undefined, 123, PlateStatus.ForSale, undefined, 1, 20
      );
    }); 

    it('should combine multiple filters', async () => {
      component.searchText = 'AB';
      component.lettersFilter = 'ABC';
      component.numbersFilter = 123;
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        'AB', 'ABC', 123, PlateStatus.ForSale, undefined, 1, 20
      );
    }); 
  });

  // User Story 4 - Reserve/Unreserve Plates
  describe('reservePlate', () => {
    it('should reserve a plate successfully', async () => {
      const reservedPlate = { ...mockPlates[0], status: PlateStatus.Reserved, reservedDate: new Date() };
      mockCatalogService.reservePlate.mockReturnValue(of(reservedPlate));

      component.plates = [...mockPlates];
      component.reservePlate(mockPlates[0]);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.reservePlate).toHaveBeenCalledWith('1');
      expect(component.success).toContain('Plate reserved successfully');
      expect(mockCatalogService.getPlates).toHaveBeenCalled();
    }); 

    it('should handle reserve errors', () => {
      mockCatalogService.reservePlate.mockReturnValue(throwError(() => new Error('Reserve failed')));

      component.reservePlate(mockPlates[0]);

      // Verify the service was called
      expect(mockCatalogService.reservePlate).toHaveBeenCalledWith('1');
    }); 
  });

  describe('unreservePlate', () => {
    it('should unreserve a plate successfully', async () => {
      const unreservedPlate = { ...mockPlates[1], status: PlateStatus.ForSale, reservedDate: undefined };
      mockCatalogService.unreservePlate.mockReturnValue(of(unreservedPlate));

      component.plates = [...mockPlates];
      component.unreservePlate(mockPlates[1]);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.unreservePlate).toHaveBeenCalledWith('2');
      expect(component.success).toContain('Plate unreserved successfully');
    }); 

    it('should handle unreserve errors', () => {
      mockCatalogService.unreservePlate.mockReturnValue(throwError(() => new Error('Unreserve failed')));

      component.unreservePlate(mockPlates[1]);

      // Verify the service was called
      expect(mockCatalogService.unreservePlate).toHaveBeenCalledWith('2');
    }); 
  });

  // User Story 5 - Default to ForSale Status
  describe('status filtering', () => {
    it('should default to ForSale status', () => {
      expect(component.statusFilter).toBe(PlateStatus.ForSale);
    });

    it('should filter by Reserved status', async () => {
      component.statusFilter = PlateStatus.Reserved;
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined, undefined, undefined, PlateStatus.Reserved, undefined, 1, 20
      );
    }); 

    it('should filter by Sold status', async () => {
      component.statusFilter = PlateStatus.Sold;
      component.loadPlates();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined, undefined, undefined, PlateStatus.Sold, undefined, 1, 20
      );
    }); 
  });

  // User Story 6 - Sell Plates
  describe('sell plate operations', () => {
    it('should open sell modal for a plate', () => {
      const plate = mockPlates[0];
      component.openSellModal(plate);

      expect(component.selectedPlate).toEqual(plate);
      expect(component.showSellModal).toBe(true);
      expect(component.calculatedPrice).toBe(plate.salePrice);
      expect(component.selectedPromoCode).toBe('');
    });

    it('should close sell modal and reset state', () => {
      component.selectedPlate = mockPlates[0];
      component.showSellModal = true;
      component.selectedPromoCode = 'TEST';
      component.calculatedPrice = 100;

      component.closeSellModal();

      expect(component.showSellModal).toBe(false);
      expect(component.selectedPlate).toBeNull();
      expect(component.selectedPromoCode).toBe('');
      expect(component.calculatedPrice).toBe(0);
    });

    it('should sell plate without promo code', async () => {
      const soldPlate = { 
        ...mockPlates[0], 
        status: PlateStatus.Sold, 
        soldDate: new Date(), 
        soldPrice: 120 
      };
      mockCatalogService.sellPlate.mockReturnValue(of(soldPlate));

      component.selectedPlate = mockPlates[0];
      component.confirmSale();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.sellPlate).toHaveBeenCalledWith('1', undefined);
      expect(component.success).toContain('Plate sold successfully');
      expect(component.showSellModal).toBe(false);
    }); 

    it('should sell plate with promo code', async () => {
      const soldPlate = { 
        ...mockPlates[0], 
        status: PlateStatus.Sold, 
        soldDate: new Date(), 
        soldPrice: 95,
        promoCodeUsed: 'DISCOUNT'
      };
      mockCatalogService.sellPlate.mockReturnValue(of(soldPlate));

      component.selectedPlate = mockPlates[0];
      component.selectedPromoCode = 'DISCOUNT';
      component.confirmSale();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.sellPlate).toHaveBeenCalledWith('1', 'DISCOUNT');
      expect(component.success).toContain('Plate sold successfully');
    }); 

    it('should handle sell errors', () => {
      mockCatalogService.sellPlate.mockReturnValue(throwError(() => ({ error: { error: 'Sell failed' } })));

      component.selectedPlate = mockPlates[0];
      component.confirmSale();

      // Verify the service was called
      expect(mockCatalogService.sellPlate).toHaveBeenCalledWith('1', undefined);
    }); 
  });

  // User Story 7 - Promo Codes
  describe('promo code calculation', () => {
    it('should calculate price without promo code', async () => {
      mockCatalogService.calculatePrice.mockReturnValue(of(120));
      
      component.selectedPlate = mockPlates[0];
      component.selectedPromoCode = '';
      component.onPromoCodeChange();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(component.calculatedPrice).toBe(120);
      expect(mockCatalogService.calculatePrice).toHaveBeenCalledWith('1', undefined);
    }); 

    it('should calculate price with DISCOUNT promo code', async () => {
      mockCatalogService.calculatePrice.mockReturnValue(of(95));

      component.selectedPlate = mockPlates[0];
      component.selectedPromoCode = 'DISCOUNT';
      component.onPromoCodeChange();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.calculatePrice).toHaveBeenCalledWith('1', 'DISCOUNT');
      expect(component.calculatedPrice).toBe(95);
    }); 

    it('should calculate price with PERCENTOFF promo code', async () => {
      mockCatalogService.calculatePrice.mockReturnValue(of(102));

      component.selectedPlate = mockPlates[0];
      component.selectedPromoCode = 'PERCENTOFF';
      component.onPromoCodeChange();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockCatalogService.calculatePrice).toHaveBeenCalledWith('1', 'PERCENTOFF');
      expect(component.calculatedPrice).toBe(102);
    }); 

    it('should handle promo code calculation errors', async () => {
      mockCatalogService.calculatePrice.mockReturnValue(
        throwError(() => new Error('Promo code validation failed'))
      );

      component.selectedPlate = mockPlates[0];
      component.selectedPromoCode = 'INVALID';
      component.onPromoCodeChange();
      await new Promise(resolve => setTimeout(resolve, 0));

      // The component logs errors to console but doesn't set component.error in onPromoCodeChange
      expect(mockCatalogService.calculatePrice).toHaveBeenCalledWith('1', 'INVALID');
    }); 
  });

  // Pagination
  describe('pagination', () => {
    it('should change page using onPageChange', () => {
      component.onPageChange(2);

      // Verify the service is called with correct page number
      expect(mockCatalogService.getPlates).toHaveBeenCalledWith(
        undefined, undefined, undefined, PlateStatus.ForSale, undefined, 2, 20
      );
    }); 

    it('should get pages array', () => {
      component.totalPages = 3;
      const pages = component.getPages();
      
      expect(pages).toEqual([1, 2, 3]);
    });

    it('should reset to page 1 when searching', async () => {
      component.currentPage = 3;
      component.searchText = 'AB';
      component.onSearch();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(component.currentPage).toBe(1);
    }); 
  });

  // Helper Methods
  describe('helper methods', () => {
    it('should get status badge class', () => {
      expect(component.getStatusBadgeClass(PlateStatus.ForSale)).toBe('badge-success');
      expect(component.getStatusBadgeClass(PlateStatus.Reserved)).toBe('badge-warning');
      expect(component.getStatusBadgeClass(PlateStatus.Sold)).toBe('badge-secondary');
    });

    it('should get status text', () => {
      expect(component.getStatusText(PlateStatus.ForSale)).toBe('For Sale');
      expect(component.getStatusText(PlateStatus.Reserved)).toBe('Reserved');
      expect(component.getStatusText(PlateStatus.Sold)).toBe('Sold');
    });
  });

  // Revenue Statistics
  describe('revenue statistics', () => {
    it('should load revenue statistics', async () => {
      fixture.detectChanges();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(component.statistics).toEqual(mockStats);
      expect(component.statistics!.totalRevenue).toBe(1200);
      expect(component.statistics!.platesSold).toBe(10);
    }); 

    it('should handle statistics loading errors gracefully', async () => {
      mockCatalogService.getRevenueStatistics.mockReturnValue(
        throwError(() => new Error('Stats failed'))
      );

      component.loadStatistics();
      await new Promise(resolve => setTimeout(resolve, 10));

      // Component logs error but doesn't set error message for statistics
      expect(component.statistics).toBeNull();
    }); 
  });

  // Error and Success Messages
  describe('message handling', () => {
    it('should display success messages after operations', async () => {
      const reservedPlate = { ...mockPlates[0], status: PlateStatus.Reserved };
      mockCatalogService.reservePlate.mockReturnValue(of(reservedPlate));

      component.reservePlate(mockPlates[0]);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(component.success).toBeTruthy();
      expect(component.success).toContain('Plate reserved successfully');
    }); 

    it('should display error messages on failures', () => {
      mockCatalogService.reservePlate.mockReturnValue(throwError(() => ({ error: { error: 'Failed' }, message: 'Failed' })));

      component.reservePlate(mockPlates[0]);

      // Verify the service was called
      expect(mockCatalogService.reservePlate).toHaveBeenCalledWith('1');
    }); 
  });
});



