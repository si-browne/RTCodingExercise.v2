import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Catalog } from './catalog';
import { Plate, PagedResult, RevenueStatistics, PlateStatus } from '../models/plate';
import { environment } from '../../environments/environment';

/**
 * Unit tests for Catalog Service
 * Testing HTTP interactions with the Catalog API according to Regtransfers Code Exercise requirements
 */
describe('Catalog Service', () => {
  let service: Catalog;
  let httpMock: HttpTestingController;
  const apiUrl = environment.catalogApiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [Catalog]
    });
    service = TestBed.inject(Catalog);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Verify that no unmatched requests are outstanding
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // User Story 1 - List Plates with 20% Markup
  describe('getPlates', () => {
    it('should fetch plates with correct API call', () => {
      const mockResponse: PagedResult<Plate> = {
        items: [
          {
            id: '1',
            registration: 'AB12 CDE',
            purchasePrice: 100,
            salePrice: 120, // 20% markup
            status: PlateStatus.ForSale,
            letters: 'AB',
            numbers: 12
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false
      };

      service.getPlates().subscribe(response => {
        expect(response).toEqual(mockResponse);
        expect(response.items.length).toBe(1);
        expect(response.items[0].salePrice).toBe(120); // Verify 20% markup
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&status=0`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should include pagination parameters', () => {
      service.getPlates(undefined, undefined, undefined, undefined, undefined, 2, 20).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=2&pageSize=20&status=0`);
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush({ items: [], totalCount: 0, page: 2, pageSize: 20, totalPages: 0, hasPreviousPage: true, hasNextPage: false });
    });
  });

  // User Story 2 - Order by Price
  describe('sorting', () => {
    it('should include sortBy parameter when provided', () => {
      service.getPlates(undefined, undefined, undefined, undefined, 'price_asc').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&status=0&sortBy=price_asc`);
      expect(req.request.params.get('sortBy')).toBe('price_asc');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });

    it('should support price_desc sorting', () => {
      service.getPlates(undefined, undefined, undefined, undefined, 'price_desc').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&status=0&sortBy=price_desc`);
      expect(req.request.params.get('sortBy')).toBe('price_desc');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });
  });

  // User Story 3 - Filter by Letters and Numbers
  describe('filtering', () => {
    it('should include search parameter when provided', () => {
      service.getPlates('AB12').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&search=AB12&status=0`);
      expect(req.request.params.get('search')).toBe('AB12');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });

    it('should include letters filter when provided', () => {
      service.getPlates(undefined, 'ABC').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&letters=ABC&status=0`);
      expect(req.request.params.get('letters')).toBe('ABC');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });

    it('should include numbers filter when provided', () => {
      service.getPlates(undefined, undefined, 123).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&numbers=123&status=0`);
      expect(req.request.params.get('numbers')).toBe('123');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });

    it('should combine multiple filters', () => {
      service.getPlates('AB', 'ABC', 123).subscribe();

      const req = httpMock.expectOne(
        `${apiUrl}/api/plates?page=1&pageSize=20&search=AB&letters=ABC&numbers=123&status=0`
      );
      expect(req.request.params.get('search')).toBe('AB');
      expect(req.request.params.get('letters')).toBe('ABC');
      expect(req.request.params.get('numbers')).toBe('123');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });
  });

  // User Story 4 - Reserve/Unreserve Plates
  describe('reservePlate', () => {
    it('should send POST request to reserve endpoint', () => {
      const plateId = 'test-id';
      const mockPlate: Plate = {
        id: plateId,
        registration: 'AB12 CDE',
        purchasePrice: 100,
        salePrice: 120,
        status: PlateStatus.Reserved,
        reservedDate: new Date()
      };

      service.reservePlate(plateId).subscribe(response => {
        expect(response.status).toBe(PlateStatus.Reserved);
        expect(response.reservedDate).toBeDefined();
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/reserve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toBeNull();
      req.flush(mockPlate);
    });
  });

  describe('unreservePlate', () => {
    it('should send POST request to unreserve endpoint', () => {
      const plateId = 'test-id';
      const mockPlate: Plate = {
        id: plateId,
        registration: 'AB12 CDE',
        purchasePrice: 100,
        salePrice: 120,
        status: PlateStatus.ForSale
      };

      service.unreservePlate(plateId).subscribe(response => {
        expect(response.status).toBe(PlateStatus.ForSale);
        expect(response.reservedDate).toBeUndefined();
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/unreserve`);
      expect(req.request.method).toBe('POST');
      req.flush(mockPlate);
    });
  });

  // User Story 5 - Default to ForSale Status
  describe('default status filter', () => {
    it('should default to ForSale status when no status provided', () => {
      service.getPlates().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&status=0`);
      expect(req.request.params.get('status')).toBe('0'); // ForSale = 0
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });

    it('should use provided status when specified', () => {
      service.getPlates(undefined, undefined, undefined, PlateStatus.Reserved).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates?page=1&pageSize=20&status=1`);
      expect(req.request.params.get('status')).toBe('1'); // Reserved = 1
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
    });
  });

  // User Story 6 - Sell Plates
  describe('sellPlate', () => {
    it('should send POST request to sell endpoint without promo code', () => {
      const plateId = 'test-id';
      const mockPlate: Plate = {
        id: plateId,
        registration: 'AB12 CDE',
        purchasePrice: 100,
        salePrice: 120,
        status: PlateStatus.Sold,
        soldDate: new Date(),
        soldPrice: 120
      };

      service.sellPlate(plateId).subscribe(response => {
        expect(response.status).toBe(PlateStatus.Sold);
        expect(response.soldPrice).toBe(120);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/sell`);
      expect(req.request.method).toBe('POST');
      req.flush(mockPlate);
    });

    it('should include promo code in request when provided', () => {
      const plateId = 'test-id';
      const promoCode = 'DISCOUNT';
      const mockPlate: Plate = {
        id: plateId,
        registration: 'AB12 CDE',
        purchasePrice: 100,
        salePrice: 120,
        status: PlateStatus.Sold,
        soldDate: new Date(),
        soldPrice: 95,
        promoCodeUsed: promoCode
      };

      service.sellPlate(plateId, promoCode).subscribe(response => {
        expect(response.promoCodeUsed).toBe(promoCode);
        expect(response.soldPrice).toBe(95);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/sell?promoCode=${promoCode}`);
      expect(req.request.params.get('promoCode')).toBe(promoCode);
      req.flush(mockPlate);
    });
  });

  describe('getRevenueStatistics', () => {
    it('should fetch revenue statistics', () => {
      const mockStats: RevenueStatistics = {
        totalRevenue: 1200,
        totalProfit: 200,
        platesSold: 10,
        averageProfitMargin: 0.1667
      };

      service.getRevenueStatistics().subscribe(stats => {
        expect(stats.totalRevenue).toBe(1200);
        expect(stats.platesSold).toBe(10);
        expect(stats.averageProfitMargin).toBeCloseTo(0.1667, 4);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/statistics/revenue`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStats);
    });
  });

  // User Story 7 - Promo Codes
  describe('calculatePrice', () => {
    it('should calculate price without promo code', () => {
      const plateId = 'test-id';
      const expectedPrice = 120;

      service.calculatePrice(plateId).subscribe(price => {
        expect(price).toBe(expectedPrice);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/calculate-price`);
      expect(req.request.method).toBe('GET');
      req.flush(expectedPrice);
    });

    it('should calculate price with DISCOUNT promo code', () => {
      const plateId = 'test-id';
      const promoCode = 'DISCOUNT';
      const expectedPrice = 95; // £120 - £25

      service.calculatePrice(plateId, promoCode).subscribe(price => {
        expect(price).toBe(expectedPrice);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/calculate-price?promoCode=${promoCode}`);
      expect(req.request.params.get('promoCode')).toBe(promoCode);
      req.flush(expectedPrice);
    });

    it('should calculate price with PERCENTOFF promo code', () => {
      const plateId = 'test-id';
      const promoCode = 'PERCENTOFF';
      const expectedPrice = 102; // £120 * 0.85

      service.calculatePrice(plateId, promoCode).subscribe(price => {
        expect(price).toBe(expectedPrice);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}/calculate-price?promoCode=${promoCode}`);
      req.flush(expectedPrice);
    });
  });

  // CRUD Operations
  describe('CRUD operations', () => {
    it('should get plate by ID', () => {
      const plateId = 'test-id';
      const mockPlate: Plate = {
        id: plateId,
        registration: 'AB12 CDE',
        purchasePrice: 100,
        salePrice: 120,
        status: PlateStatus.ForSale
      };

      service.getPlateById(plateId).subscribe(plate => {
        expect(plate.id).toBe(plateId);
        expect(plate.registration).toBe('AB12 CDE');
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPlate);
    });

    it('should create a new plate', () => {
      const newPlate: Plate = {
        id: '',
        registration: 'XY99 ZZZ',
        purchasePrice: 200,
        salePrice: 0,
        status: PlateStatus.ForSale
      };

      const createdPlate: Plate = {
        ...newPlate,
        id: 'new-id',
        salePrice: 240 // 20% markup
      };

      service.createPlate(newPlate).subscribe(plate => {
        expect(plate.id).toBe('new-id');
        expect(plate.salePrice).toBe(240);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newPlate);
      req.flush(createdPlate);
    });

    it('should update an existing plate', () => {
      const plateId = 'test-id';
      const updatedPlate: Plate = {
        id: plateId,
        registration: 'AB12 CDE',
        purchasePrice: 150,
        salePrice: 180,
        status: PlateStatus.ForSale
      };

      service.updatePlate(plateId, updatedPlate).subscribe(plate => {
        expect(plate.purchasePrice).toBe(150);
        expect(plate.salePrice).toBe(180);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updatedPlate);
      req.flush(updatedPlate);
    });

    it('should delete a plate', () => {
      const plateId = 'test-id';

      service.deletePlate(plateId).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/api/plates/${plateId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
