import { TestBed } from '@angular/core/testing';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { NotificationService } from './notification';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    vi.useFakeTimers();

    TestBed.configureTestingModule({
      providers: [NotificationService],
    });

    service = TestBed.inject(NotificationService);
  });

  afterEach(() => {
    service.clear();
    vi.useRealTimers();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should show a notification with the provided values', () => {
    service.show(
      'info',
      'This is an information message.',
      'Information',
      5000,
    );

    const notification = service.notification();

    expect(notification).toEqual(
      expect.objectContaining({
        type: 'info',
        title: 'Information',
        message: 'This is an information message.',
      }),
    );

    expect(notification?.id).toEqual(
      expect.any(Number),
    );
  });

  it('should clear the notification automatically after the duration', () => {
    service.show(
      'success',
      'Operation completed.',
      'Success',
      5000,
    );

    expect(service.notification()).not.toBeNull();

    vi.advanceTimersByTime(4999);

    expect(service.notification()).not.toBeNull();

    vi.advanceTimersByTime(1);

    expect(service.notification()).toBeNull();
  });

  it('should show an error notification with the default title', () => {
    service.showError('Something failed.');

    expect(service.notification()).toEqual(
      expect.objectContaining({
        type: 'error',
        title: 'Something went wrong',
        message: 'Something failed.',
      }),
    );
  });

  it('should show a success notification with a custom title', () => {
    service.showSuccess(
      'Task created successfully.',
      'Task created',
    );

    expect(service.notification()).toEqual(
      expect.objectContaining({
        type: 'success',
        title: 'Task created',
        message: 'Task created successfully.',
      }),
    );
  });

  it('should show an information notification with the default title', () => {
    service.showInfo('Please review the information.');

    expect(service.notification()).toEqual(
      expect.objectContaining({
        type: 'info',
        title: 'Information',
        message: 'Please review the information.',
      }),
    );
  });

  it('should clear the current notification manually', () => {
    service.showInfo('Temporary message.');

    service.clear();

    expect(service.notification()).toBeNull();
  });

  it('should cancel the previous timeout when showing a new notification', () => {
    service.show(
      'info',
      'First message.',
      'First',
      100,
    );

    service.show(
      'success',
      'Second message.',
      'Second',
      200,
    );

    vi.advanceTimersByTime(100);

    expect(service.notification()).toEqual(
      expect.objectContaining({
        message: 'Second message.',
      }),
    );

    vi.advanceTimersByTime(100);

    expect(service.notification()).toBeNull();
  });

  it('should cancel the timeout when clearing manually', () => {
    service.show(
      'info',
      'Temporary message.',
      'Information',
      1000,
    );

    service.clear();

    vi.advanceTimersByTime(1000);

    expect(service.notification()).toBeNull();
  });
});