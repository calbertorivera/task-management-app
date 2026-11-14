import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import {
  NotificationMessage,
  NotificationService,
} from '../../../core/notifications/notification';
import { Toast } from './toast';

describe('Toast', () => {
  let component: Toast;
  let fixture: ComponentFixture<Toast>;

  const notificationSignal =
    signal<NotificationMessage | null>(null);

  const notificationServiceMock = {
    notification: notificationSignal.asReadonly(),
    clear: vi.fn(),
  };

  beforeEach(async () => {
    notificationSignal.set(null);
    notificationServiceMock.clear.mockReset();

    await TestBed.configureTestingModule({
      imports: [Toast],
      providers: [
        {
          provide: NotificationService,
          useValue: notificationServiceMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Toast);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should expose the notification signal from the service', () => {
    const componentNotification = (
      component as unknown as {
        notification: typeof notificationSignal;
      }
    ).notification;

    expect(componentNotification()).toBeNull();
  });

  it('should clear the notification when closed', () => {
    (
      component as unknown as {
        close: () => void;
      }
    ).close();

    expect(
      notificationServiceMock.clear,
    ).toHaveBeenCalledOnce();
  });
});