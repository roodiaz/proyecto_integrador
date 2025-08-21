import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateAlert } from './create-alert';

describe('CreateAlert', () => {
  let component: CreateAlert;
  let fixture: ComponentFixture<CreateAlert>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateAlert]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateAlert);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
